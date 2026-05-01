using Application.Common;
using Application.DTOs;
using Application.Gateways;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Entities.Order;
using Domain.Events.CreateOrder;
using Domain.Interfaces;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.CreateOrder;

public class CreateOrderCommandHandler
    (
        IOrderPersistenceService orderPersistenceService,
        IIdempotencyRepository idempotencyRepository,
    IWriteRegionOwnershipResolver writeRegionOwnershipResolver,
        IProductGateway productGateway,
        IUserGateway userGateway,
        ILogger<CreateOrderCommandHandler> logger
        ) : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var ownership = writeRegionOwnershipResolver.ResolveForCustomer(request.CustomerId);
            if (ownership.IsEnabled && !ownership.IsCurrentRegionOwner)
            {
                logger.LogWarning(
                    "CreateOrder rejected in region {CurrentRegion}. Owner region for customer {CustomerId} is {OwnerRegion}",
                    ownership.CurrentRegion,
                    request.CustomerId,
                    ownership.OwnerRegion);

                return Result<Guid>.Failure(
                    $"Write ownership mismatch. Current region '{ownership.CurrentRegion}' is not owner for customer {request.CustomerId}. " +
                    $"Forward to owner region '{ownership.OwnerRegion}'.");
            }

            var existingRecord = await idempotencyRepository.GetByKeyAsync(
                request.IdempotencyKey,
                cancellationToken);

            if (existingRecord != null)
            {
                logger.LogInformation(
                    "Duplicate request detected for idempotency key {Key}. " +
                    "Returning existing order {OrderId}",
                    request.IdempotencyKey,
                    existingRecord.ResultId);

                return Result<Guid>.Success(existingRecord.ResultId);
            }

            var customerProfile = await userGateway.GetUserProfileAsync(
                request.CustomerId,
                cancellationToken);

            if (!customerProfile.IsActive)
            {
                return Result<Guid>.Failure(
                    $"Customer {request.CustomerId} is blocked and cannot create orders");
            }
            
            var customerId = CustomerId.From(request.CustomerId);
           
            var address = Address.Create(
                request.DeliveryAddress.Street,
                request.DeliveryAddress.City,
                request.DeliveryAddress.Country,
                request.DeliveryAddress.PostalCode
            );

           var productIds = request.Items.Select(i => i.ProductId);
            var authorizedPrices = await productGateway.GetCurrentPricesAsync(
                productIds, cancellationToken);

            var priceIndex = authorizedPrices.ToDictionary(p => p.ProductId);

            var orderItems = request.Items.Select(item =>
            {
                var canonical = priceIndex[item.ProductId];
                return OrderItem.Create(
                    ProductId.From(item.ProductId),
                    item.Quantity,
                    Money.Create(canonical.Price, canonical.Currency));
            }).ToList();

            var order = Order.Create(customerId, address, orderItems, request.PaymentIntentId, request.PaymentMethod);

            logger.LogInformation(
                "Created order aggregate {OrderId} for customer {CustomerId}",
                order.Id.Value,
                customerId.Value
            );

            await orderPersistenceService.CreateOrderAsync(
                order,
                request.IdempotencyKey,
                cancellationToken);
            
                logger.LogInformation(
                    "Saved {EventCount} event to outbox for order {OrderId}",
                    order.UncommitedEvents.Count,
                    order.Id.Value
                );

            return Result<Guid>.Success(order.Id.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failure to create order");
            return Result<Guid>.Failure($"Failed to create order: {ex.Message}");
        }
    }
}
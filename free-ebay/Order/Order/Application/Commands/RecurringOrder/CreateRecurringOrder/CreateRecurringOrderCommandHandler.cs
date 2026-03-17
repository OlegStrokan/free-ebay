using Application.Common;
using Application.DTOs;
using Application.Gateways;
using Application.Interfaces;
using Domain.Entities.Subscription;
using Domain.Interfaces;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.RecurringOrder.CreateRecurringOrder;

public sealed class CreateRecurringOrderCommandHandler(
    IRecurringOrderPersistenceService persistenceService,
    IIdempotencyRepository idempotencyRepository,
    IUserGateway userGateway,
    ILogger<CreateRecurringOrderCommandHandler> logger)
    : IRequestHandler<CreateRecurringOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateRecurringOrderCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existing = await idempotencyRepository.GetByKeyAsync(
                    request.IdempotencyKey, cancellationToken);
                if (existing != null)
                {
                    logger.LogInformation(
                        "Duplicate CreateRecurringOrder request for key {Key}. Returning existing {Id}",
                        request.IdempotencyKey, existing.ResultId);
                    return Result<Guid>.Success(existing.ResultId);
                }
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
            var frequency = ScheduleFrequency.FromName(request.Frequency);
            var address = Address.Create(
                request.DeliveryAddress.Street,
                request.DeliveryAddress.City,
                request.DeliveryAddress.Country,
                request.DeliveryAddress.PostalCode);

            var items = request.Items.Select(i =>
                RecurringOrderItem.Create(
                    ProductId.From(i.ProductId),
                    i.Quantity,
                    Money.Create(i.Price, i.Currency))).ToList();

            var order = Domain.Entities.Subscription.RecurringOrder.Create(
                customerId, frequency, items, address,
                request.PaymentMethod, request.FirstRunAt, request.MaxExecutions);

            var key = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? $"recurring-create-{order.Id.Value}"
                : request.IdempotencyKey;

            await persistenceService.CreateAsync(order, key, cancellationToken);

            logger.LogInformation(
                "RecurringOrder {Id} created for customer {CustomerId} with frequency '{Frequency}'. Next run: {NextRunAt}",
                order.Id.Value, request.CustomerId, request.Frequency, order.NextRunAt);

            return Result<Guid>.Success(order.Id.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating RecurringOrder for customer {CustomerId}", request.CustomerId);
            return Result<Guid>.Failure(ex.Message);
        }
    }
}

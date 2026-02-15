using System.Text.Json;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Events.OrderReturn;
using Domain.Interfaces;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.RequestReturn;

public class RequestReturnCommandHandler(
    IOrderRepository orderRepository,
    IIdempotencyRepository idempotencyRepository,
    IReturnRequestPersistenceService returnRequestPersistenceService,
    ILogger<RequestReturnCommandHandler> logger
        ) : IRequestHandler<RequestReturnCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        RequestReturnCommand request,
        CancellationToken cancellationToken)
    {
        try
        {

            var existingRecord = await idempotencyRepository.GetByKeyAsync(
                request.IdempotencyKey,
                cancellationToken);

            if (existingRecord != null)
            {
                logger.LogInformation(
                    "Duplicate return request detected for idempotency key {Key}. " +
                    "Returning existing order {OrderId}",
                    request.IdempotencyKey,
                    existingRecord.ResultId);

                return Result<Guid>.Success(existingRecord.ResultId);
            }
            
            logger.LogInformation(
                "Processing return request for order {OrderId}",
                request.OrderId);
            
                var order = await orderRepository.GetByIdAsync(
                    OrderId.From(request.OrderId),
                    cancellationToken);

                if (order == null)
                {
                    return Result<Guid>.Failure($"Order {request.OrderId} not found");
                }
                
                if (order.Status != OrderStatus.Completed)
                    return Result<Guid>.Failure(
                        $"Order {request.OrderId} must be completed to request return. " +
                        $"Current status: {order.Status}");

                var itemsToReturn = request.ItemsToReturn.Select(dto =>
                    OrderItem.Create(
                        ProductId.From(dto.ProductId),
                        dto.Quantity,
                        Money.Create(dto.Price, dto.Currency)
                    )).ToList();

                var refundAmount = Order.CalculateTotalPrice(itemsToReturn);

                var returnWindow = TimeSpan.FromDays(14);

                var returnRequest = ReturnRequest.Create(
                    OrderId.From(request.OrderId),
                    order.CustomerId,
                    request.Reason,
                    itemsToReturn,
                    refundAmount,
                    order.CompletedAt!.Value, // already not null
                    order.Items.ToList(),
                    returnWindow);

                logger.LogInformation(
                    "ReturnRequest created with ID  {ReturnRequestId} for order {OrderId}",
                   returnRequest.Id.Value, order.Id.Value);

                var resultOrderId = await returnRequestPersistenceService.CreateReturnRequestAsync(
                    returnRequest,
                    request.IdempotencyKey,
                    order.Id.Value,
                    cancellationToken);
                
                logger.LogInformation(
                    "ReturnRequest saved successfully with {EventCount} events for order {OrderId}",
                    returnRequest.UncommitedEvents.Count,
                    resultOrderId);

                return Result<Guid>.Success(returnRequest.Id.Value);
        }

        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process for return request for order {OrderId}", request.OrderId);
            return Result<Guid>.Failure($"Failed to request return: {ex.Message}");
        }
    }
}
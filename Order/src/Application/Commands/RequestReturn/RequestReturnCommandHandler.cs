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
    IReturnRequestRepository returnRequestRepository,
    IIdempotencyRepository idempotencyRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork,
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

            await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var order = await orderRepository.GetByIdAsync(
                    OrderId.From(request.OrderId),
                    cancellationToken);

                if (order == null)
                {
                    return Result<Guid>.Failure($"Order {request.OrderId} not found");
                }

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
                    order.CompletedAt.Value,
                    order.Items.ToList(),
                    returnWindow);
                
                

                logger.LogInformation(
                    "ReturnRequest created with ID  {ReturnRequestId} for order {OrderId}",
                   returnRequest.Id.Value, order.Id.Value);

                await returnRequestRepository.AddAsync(returnRequest, cancellationToken);

                foreach (var domainEvent in returnRequest.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        domainEvent.EventId,
                        domainEvent.GetType().Name,
                        SerializeEvent(domainEvent),
                        domainEvent.OccurredOn,
                        cancellationToken);
                }

                await idempotencyRepository.SaveAsync(
                    request.IdempotencyKey,
                    order.Id.Value,
                    DateTime.UtcNow,
                    cancellationToken);

                logger.LogInformation(
                    "Saved {EventCount} events to outbox and idempotency key for return request on order {OrderId}",
                    order.UncommitedEvents.Count,
                    order.Id.Value);

                await unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                order.MarkEventsAsCommited();

                logger.LogInformation(
                    "Return request transaction committed for order {OrderId}",
                    order.Id.Value);

                return Result<Guid>.Success(order.Id.Value);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process for return request for order {OrderId}", request.OrderId);
            return Result<Guid>.Failure($"Failed to request return: {ex.Message}");
        }
    }

    private string SerializeEvent(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            ReturnRequestCreatedEvent e => JsonSerializer.Serialize(new ReturnRequestCreatedEventDto
            {
                OrderId = e.OrderId.Value,
                CustomerId = e.CustomerId.Value,
                Reason = e.Reason,
                ItemsToReturn = e.ItemsToReturn.Select(i => new OrderItemDto(
                    i.ProductId.Value,
                    i.Quantity,
                    i.PriceAtPurchase.Amount,
                    i.PriceAtPurchase.Currency
                )).ToList(),
                RefundAmount = e.RefundAmount.Amount,
                Currency = e.RefundAmount.Currency,
                RequestedAt = e.RequestedAt
            }),
            _ => JsonSerializer.Serialize(domainEvent)
        };
    }
}
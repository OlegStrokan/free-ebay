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

                order.RequestReturn(request.Reason, itemsToReturn);

                logger.LogInformation(
                    "Return request created for order {OrderId} with {ItemCount} items",
                    order.Id.Value,
                    itemsToReturn.Count);

                await orderRepository.AddAsync(order, cancellationToken);

                foreach (var domainEvent in order.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        domainEvent.EventId,
                        domainEvent.GetType().Name,
                        SerializeEvent(domainEvent),
                        domainEvent.OccurredOn,
                        cancellationToken);
                }

                logger.LogInformation(
                    "Saved {EventCount} event(s) to outbox for return request on order {OrderId}",
                    order.UncommitedEvents.Count,
                    order.Id.Value);

                await unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                order.MarkEventsAsCommited();

                logger.LogInformation(
                    "Return request transaction commited for order {OrderId}",
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
            logger.LogError(ex, "Failed to process for order {OrderId}", request.OrderId);
            return Result<Guid>.Failure($"Failed to request return: {ex.Message}");
        }
    }

    private string SerializeEvent(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            OrderReturnRequestedEvent e => JsonSerializer.Serialize(new OrderReturnRequestedEventDto
            {
                OrderId = e.OrderId.Value,
                CustomerId = e.CustomerId.Value,
                Reason = e.Reason,
                ItemToReturn = e.ItemToReturn.Select(i => new OrderItemDto(
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
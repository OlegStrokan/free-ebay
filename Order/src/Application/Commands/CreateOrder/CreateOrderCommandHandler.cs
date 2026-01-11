using System.Text.Json;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Sagas;
using Domain.Common;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.CreateOrder;

public class CreateOrderCommandHandler
    (
        IOrderRepository orderRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateOrderCommandHandler> logger
        ) : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var customerId = CustomerId.From(request.CustomerId);
            var address = Address.Create(
                request.DeliveryAddress.Street,
                request.DeliveryAddress.City,
                request.DeliveryAddress.Country,
                request.DeliveryAddress.PostalCode
            );

            var orderItems = request.Items.Select(item =>
                OrderItem.Create(
                    ProductId.From(item.ProductId),
                    item.Quantity,
                    Money.Create(item.Price, item.Currency))).ToList();

            var order = Order.Create(customerId, address, orderItems);

            logger.LogInformation(
                "Created order aggregate {OrderId} for customer {CustomerId}",
                order.Id.Value,
                customerId.Value
            );


            await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                await orderRepository.SaveAsync(order, cancellationToken);

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
                    "Saved {EventCount} event to outbox for order {OrderId}",
                    order.UncommitedEvents.Count,
                    order.Id.Value
                );

                await transaction.CommitAsync(cancellationToken);

                order.MarkEventsAsCommited();

                logger.LogInformation(
                    "Transaction committed for order {OrderId}",
                    order.Id.Value
                );

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
            logger.LogError(ex, "Failure to create order");
            return Result<Guid>.Failure($"Failed to create order: {ex.Message}");
        }
    }

    private string SerializeEvent(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            OrderCreatedEvent e => JsonSerializer.Serialize(new OrderCreatedEventDto
            {
                OrderId = e.OrderId.Value,
                CustomerId = e.CustomerId.Value,
                TotalAmount = e.TotalPrice.Amount,
                Currency = e.TotalPrice.Currency,
                DeliveryAddress = new AddressDto(
                    e.DeliveryAddress.Street,
                    e.DeliveryAddress.City,
                    e.DeliveryAddress.Country,
                    e.DeliveryAddress.PostalCode
                ),
                Items = e.Items.Select(i => new OrderItemDto(
                    i.ProductId.Value,
                    i.Quantity,
                    i.PriceAtPurchase.Amount,
                    i.PriceAtPurchase.Currency)).ToList(),
                CreatedAt = e.CreatedAt
            }),
            _ => JsonSerializer.Serialize(domainEvent)
        };
    }
}
// trigger gprc method
// call command handler => create entity, save to db, save to outbox table
// background worker async monitor outbox table => send to kafka
// background consumer worker saga receive message, start saga and do shit ton of stuff
// return all
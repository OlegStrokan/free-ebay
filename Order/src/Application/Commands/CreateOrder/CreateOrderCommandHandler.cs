using Application.Common;
using Application.Interfaces;
using Application.Sagas;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.CreateOrder;

public class CreateOrderCommandHandler
    (
        IOrderRepository orderRepository,
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

            await orderRepository.SaveAsync(order, cancellationToken);

            logger.LogInformation(
                "Saved {EventCount} event for order {OrderId}",
                order.UncommitedEvents.Count,
                order.Id.Value
            );
            
            order.MarkEventsAsCommited();
            
            logger.LogInformation(
                "Order {OrderId} created successfully. Saga will be triggered by event.",
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
// trigger gprc method
// call command handler => create entity, save to db, save to outbox table
// background worker async monitor outbox table => send to kafka
// background consumer worker saga receive message, start saga and do shit ton of stuff
// return all
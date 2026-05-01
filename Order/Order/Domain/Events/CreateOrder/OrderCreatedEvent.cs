using Domain.Common;
using Domain.Entities;
using Domain.Entities.Order;
using Domain.ValueObjects;

namespace Domain.Events.CreateOrder;

public sealed record OrderCreatedEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    Money TotalPrice,
    Address DeliveryAddress,
    List<OrderItem> Items,
    DateTime CreatedAt,
    string? ProviderPaymentIntentId = null,
    string? PaymentMethod = null) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => CreatedAt;
}
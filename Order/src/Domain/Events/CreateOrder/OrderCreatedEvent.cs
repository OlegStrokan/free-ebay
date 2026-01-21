using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Events.CreateOrder;

public sealed record OrderCreatedEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    Money TotalPrice,
    Address DeliveryAddress,
    List<OrderItem> Items,
    DateTime CreatedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
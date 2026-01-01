using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record OrderCreatedEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    Money Amount,
    Address DeliveryAddress,
    List<OrderItem> Items,
    DateTime PaidAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
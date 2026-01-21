using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.CreateOrder;

public sealed record OrderCancelledEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    DateTime CancelledAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
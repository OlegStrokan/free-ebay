using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.CreateOrder;

public sealed record OrderCompletedEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    DateTime CompletedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.Subscription;

public sealed record RecurringOrderPausedEvent(
    RecurringOrderId RecurringOrderId,
    DateTime PausedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => PausedAt;
}

using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.Subscription;

public sealed record RecurringOrderCancelledEvent(
    RecurringOrderId RecurringOrderId,
    string Reason,
    DateTime CancelledAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => CancelledAt;
}

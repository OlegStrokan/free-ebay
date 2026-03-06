using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.Subscription;


public sealed record RecurringOrderExecutionFailedEvent(
    RecurringOrderId RecurringOrderId,
    string Reason,
    DateTime NextRetryAt,
    DateTime FailedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => FailedAt;
}

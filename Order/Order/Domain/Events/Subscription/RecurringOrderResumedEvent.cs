using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.Subscription;

public sealed record RecurringOrderResumedEvent(
    RecurringOrderId RecurringOrderId,
    DateTime NextRunAt,
    DateTime ResumedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => ResumedAt;
}

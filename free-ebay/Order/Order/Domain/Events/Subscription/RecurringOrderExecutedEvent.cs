using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.Subscription;

public sealed record RecurringOrderExecutedEvent(
    RecurringOrderId RecurringOrderId,
    Guid CreatedOrderId,
    int ExecutionNumber,
    DateTime NextRunAt,
    DateTime ExecutedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => ExecutedAt;
}

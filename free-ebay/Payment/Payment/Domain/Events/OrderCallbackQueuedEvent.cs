using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record OrderCallbackQueuedEvent(
    PaymentId PaymentId,
    string CallbackEventId,
    string CallbackType,
    string OrderId,
    DateTime QueuedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn => QueuedAt;
}
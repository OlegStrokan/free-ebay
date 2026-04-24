using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record PaymentFailedEvent(
    PaymentId PaymentId,
    FailureReason FailureReason,
    DateTime FailedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn => FailedAt;
}
using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record RefundFailedEvent(
    PaymentId PaymentId,
    RefundId RefundId,
    FailureReason FailureReason,
    DateTime FailedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn => FailedAt;
}
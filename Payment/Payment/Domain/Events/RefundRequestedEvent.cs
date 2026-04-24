using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record RefundRequestedEvent(
    PaymentId PaymentId,
    RefundId RefundId,
    Money Amount,
    string Reason,
    DateTime RequestedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn => RequestedAt;
}
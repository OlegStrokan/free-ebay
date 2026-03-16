using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record RefundSucceededEvent(
    PaymentId PaymentId,
    RefundId RefundId,
    ProviderRefundId? ProviderRefundId,
    DateTime SucceededAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn => SucceededAt;
}
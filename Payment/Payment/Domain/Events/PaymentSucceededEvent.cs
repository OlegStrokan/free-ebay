using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record PaymentSucceededEvent(
    PaymentId PaymentId,
    ProviderPaymentIntentId? ProviderPaymentIntentId,
    DateTime SucceededAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn => SucceededAt;
}
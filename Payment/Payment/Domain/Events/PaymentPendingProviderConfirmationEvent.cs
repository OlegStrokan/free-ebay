using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record PaymentPendingProviderConfirmationEvent(
    PaymentId PaymentId,
    ProviderPaymentIntentId ProviderPaymentIntentId,
    DateTime PendingAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn => PendingAt;
}
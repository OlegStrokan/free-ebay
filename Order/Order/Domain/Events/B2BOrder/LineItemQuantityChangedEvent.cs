using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.B2BOrder;

public sealed record LineItemQuantityChangedEvent(
    B2BOrderId B2BOrderId,
    QuoteLineItemId LineItemId,
    int NewQuantity,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

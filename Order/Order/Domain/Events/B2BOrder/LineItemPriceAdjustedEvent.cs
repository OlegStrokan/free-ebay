using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.B2BOrder;

public sealed record LineItemPriceAdjustedEvent(
    B2BOrderId B2BOrderId,
    QuoteLineItemId LineItemId,
    Money NewPrice,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

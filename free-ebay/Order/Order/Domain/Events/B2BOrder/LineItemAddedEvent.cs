using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.B2BOrder;

public sealed record LineItemAddedEvent(
    B2BOrderId B2BOrderId,
    QuoteLineItemId LineItemId,
    ProductId ProductId,
    int Quantity,
    Money UnitPrice,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

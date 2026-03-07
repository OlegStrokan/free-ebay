using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record ProductStockUpdatedEvent(
    ProductId ProductId,
    int       PreviousQuantity,
    int       NewQuantity,
    DateTime  OccurredOn) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}

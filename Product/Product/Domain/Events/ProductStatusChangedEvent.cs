using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record ProductStatusChangedEvent(
    ProductId ProductId,
    string PreviousStatus,
    string NewStatus,
    DateTime OccurredOn) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}

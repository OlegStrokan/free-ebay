using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record ProductDeletedEvent(
    ProductId ProductId,
    DateTime OccurredOn) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}

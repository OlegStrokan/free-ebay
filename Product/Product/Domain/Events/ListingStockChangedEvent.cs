using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record ListingStockChangedEvent(
    ListingId ListingId,
    int PreviousQuantity,
    int NewQuantity,
    DateTime ChangedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => ChangedAt;
}
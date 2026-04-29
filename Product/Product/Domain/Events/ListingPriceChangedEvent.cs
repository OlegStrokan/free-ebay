using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record ListingPriceChangedEvent(
    ListingId ListingId,
    Money PreviousPrice,
    Money NewPrice,
    DateTime ChangedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => ChangedAt;
}
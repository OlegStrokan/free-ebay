using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record ListingStatusChangedEvent(
    ListingId ListingId,
    string PreviousStatus,
    string NewStatus,
    DateTime ChangedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => ChangedAt;
}
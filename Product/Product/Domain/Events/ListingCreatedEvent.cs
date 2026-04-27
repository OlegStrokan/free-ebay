using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record ListingCreatedEvent(
    ListingId ListingId,
    CatalogItemId CatalogItemId,
    SellerId SellerId,
    Money Price,
    int InitialStock,
    ListingCondition Condition,
    ListingStatus Status,
    string? SellerNotes,
    DateTime CreatedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => CreatedAt;
}
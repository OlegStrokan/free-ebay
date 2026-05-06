using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record CatalogItemListingSummaryUpdatedEvent(
    CatalogItemId CatalogItemId,
    decimal MinPrice,
    string MinPriceCurrency,
    int SellerCount,
    bool HasActiveListings,
    string? BestCondition,
    int TotalStock,
    DateTime UpdatedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => UpdatedAt;
}

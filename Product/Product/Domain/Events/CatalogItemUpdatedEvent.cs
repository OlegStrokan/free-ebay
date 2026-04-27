using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record CatalogItemUpdatedEvent(
    CatalogItemId CatalogItemId,
    string Name,
    string Description,
    CategoryId CategoryId,
    string? Gtin,
    List<ProductAttribute> Attributes,
    List<string> ImageUrls,
    DateTime UpdatedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => UpdatedAt;
}
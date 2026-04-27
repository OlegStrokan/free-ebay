using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record CatalogItemCreatedEvent(
    CatalogItemId CatalogItemId,
    string Name,
    string Description,
    CategoryId CategoryId,
    string? Gtin,
    List<ProductAttribute> Attributes,
    List<string> ImageUrls,
    DateTime CreatedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => CreatedAt;
}
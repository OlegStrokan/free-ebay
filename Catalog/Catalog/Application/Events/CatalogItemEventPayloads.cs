namespace Application.Events;

public sealed record CatalogItemIdPayload
{
    public Guid Value { get; init; }
}

public sealed record CatalogItemCreatedEventPayload
{
    public required CatalogItemIdPayload CatalogItemId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required CategoryIdPayload CategoryId { get; init; }
    public string? Gtin { get; init; }
    public required List<ProductAttributePayload> Attributes { get; init; }
    public required List<string> ImageUrls { get; init; }
    public required DateTime CreatedAt { get; init; }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
}

public sealed record CatalogItemUpdatedEventPayload
{
    public required CatalogItemIdPayload CatalogItemId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required CategoryIdPayload CategoryId { get; init; }
    public string? Gtin { get; init; }
    public required List<ProductAttributePayload> Attributes { get; init; }
    public required List<string> ImageUrls { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
}

public sealed record CatalogItemListingSummaryUpdatedEventPayload
{
    public required CatalogItemIdPayload CatalogItemId { get; init; }
    public required decimal MinPrice { get; init; }
    public required string MinPriceCurrency { get; init; }
    public required int SellerCount { get; init; }
    public required bool HasActiveListings { get; init; }
    public string? BestCondition { get; init; }
    public required int TotalStock { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
}

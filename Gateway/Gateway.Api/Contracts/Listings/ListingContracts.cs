namespace Gateway.Api.Contracts.Listings;

public sealed record ListingDetailResponse(
    string ListingId,
    string Name,
    string Description,
    string CategoryId,
    string CategoryName,
    decimal Price,
    string Currency,
    int Stock,
    IReadOnlyList<ListingAttributeResponse> Attributes,
    IReadOnlyList<string> ImageUrls,
    string CatalogItemId,
    string SellerId,
    string Status,
    string Condition,
    string? Gtin,
    string? SellerNotes);

public sealed record ListingAttributeResponse(string Key, string Value);

public sealed record GetListingsForCatalogItemResponse(
    IReadOnlyList<ListingDetailResponse> Listings,
    int TotalCount);

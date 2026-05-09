namespace Gateway.Api.Contracts.UserEvents;

public sealed record ProductViewedRequest(
    string CatalogItemId,
    int DurationMs = 0,
    string Source = "direct",
    string? Category = null,
    string? Brand = null,
    double? Price = null,
    string? Condition = null);

public sealed record ProductClickedRequest(
    string CatalogItemId,
    string QueryText,
    int Rank = 0,
    string? Category = null,
    string? Brand = null,
    double? Price = null,
    string? Condition = null);

public sealed record PurchaseCompletedRequest(
    string CatalogItemId,
    string? ListingId = null,
    double? Price = null,
    string? Category = null,
    string? Brand = null,
    string? Condition = null);

public sealed record SearchBouncedRequest(
    string QueryText);

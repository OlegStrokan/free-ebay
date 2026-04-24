namespace Gateway.Api.Contracts.Search;

public sealed record SearchResponse(
    IReadOnlyList<SearchResultItemResponse> Items,
    int TotalCount,
    int Page,
    int Size,
    bool WasAiSearch,
    string? ParsedQueryDebug);

public sealed record SearchResultItemResponse(
    string ProductId,
    string Name,
    string Category,
    double Price,
    string Currency,
    double RelevanceScore,
    IReadOnlyList<string> ImageUrls);

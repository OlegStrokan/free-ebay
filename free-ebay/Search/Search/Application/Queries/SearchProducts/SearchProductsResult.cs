namespace Application.Queries.SearchProducts;

public sealed record SearchProductsResult(
    List<ProductSearchItem> Items,
    int TotalCount,
    int Page,
    int Size,
    // @think: is this the stupidest field name that I wrote for my entire miserable life?
    bool WasAiSearch,
    string? ParsedQueryDebug
    );
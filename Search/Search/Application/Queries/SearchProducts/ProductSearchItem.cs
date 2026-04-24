namespace Application.Queries.SearchProducts;

public sealed record ProductSearchItem(
    Guid ProductId,
    string Name,
    string Category,
    decimal Price,
    string Currency,
    double RelevanceScore,
    List<string> ImageUrls
    );
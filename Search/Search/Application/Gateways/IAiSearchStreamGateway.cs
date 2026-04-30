using Application.Queries.SearchProducts;

namespace Application.Gateways;

public interface IAiSearchStreamGateway
{
    IAsyncEnumerable<StreamSearchResult> SearchStreamAsync(
        string query, int page, int size, CancellationToken ct);
}

public sealed record StreamSearchResult(
    List<ProductSearchItem> Items,
    int TotalCount,
    bool WasAiSearch,
    SearchResultPhase Phase
);

public enum SearchResultPhase
{
    Keyword = 0,
    Merged = 1
}

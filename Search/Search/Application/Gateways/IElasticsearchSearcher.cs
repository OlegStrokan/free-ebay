using Application.Queries.SearchProducts;

namespace Application.Gateways;

public interface IElasticsearchSearcher
{
    Task<SearchProductsResult> SearchAsync(
        SearchProductsQuery query,
        CancellationToken ct);
}
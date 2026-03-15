using Application.Queries.SearchProducts;

namespace Application.Gateways;

public interface IElasticSearcher
{
    Task<SearchProductsResult> SearchAsync(
        SearchProductsQuery query,
        CancellationToken ct);
}
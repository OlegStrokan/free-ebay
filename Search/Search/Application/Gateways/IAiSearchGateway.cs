using Application.Queries.SearchProducts;

namespace Application.Gateways;

public interface IAiSearchGateway
{
    Task<SearchProductsResult> SearchAsync(
        SearchProductsQuery query, CancellationToken ct);
}
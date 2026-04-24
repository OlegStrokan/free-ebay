using Application.Gateways;
using Application.Queries.SearchProducts;

namespace Infrastructure.AiSearch;

// phase 0, registered when AiSearch:Enabled = false,
// always throw handler catches and falls back to elastic.
public sealed class NullAiSearchGateway : IAiSearchGateway
{
    public Task<SearchProductsResult> SearchAsync(
        SearchProductsQuery query,
        CancellationToken   ct)
        => throw new NotSupportedException(
            "AI search is disabled. Set AiSearch:Enabled = true (Phase 3).");
}
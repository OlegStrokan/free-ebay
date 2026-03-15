using Application.Gateways;
using Application.Queries.SearchProducts;
using Microsoft.Extensions.Logging;
using Domain.Common.Interfaces;

namespace Application.Queries.SearchProducts;

public sealed class SearchProductsQueryHandler(
    IElasticSearcher _elasticSearch,
    IAiSearchGateway _aiGateway,
    ILogger _logger)
: IQueryHandler<SearchProductsQuery, SearchProductsResult>
{
    
    private static readonly TimeSpan AiTimeout = TimeSpan.FromMilliseconds(500);
    
    public async Task<SearchProductsResult> HandleAsync(SearchProductsQuery query, CancellationToken ct)
    {
        if (query.UseAi)
        {
            try
            {
                return await _aiGateway
                    .SearchAsync(query, ct)
                    .WaitAsync(AiTimeout, ct);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning(
                    "AI search timed out after {Ms}ms for query [{Query}]. " +
                    "Falling back to Elasticsearch.",
                    AiTimeout.TotalMilliseconds,
                    query.QueryText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "AI search threw an exception for query [{Query}]. " +
                    "Falling back to Elasticsearch.",
                    query.QueryText);
            }
        }

        return await _elasticSearch.SearchAsync(query, ct);
    }
}
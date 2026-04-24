using Application.Gateways;
using Microsoft.Extensions.Logging;
using Domain.Common.Interfaces;

namespace Application.Queries.SearchProducts;

public sealed class SearchProductsQueryHandler(
    IElasticsearchSearcher elasticsearchSearcher,
    IAiSearchGateway aiGateway,
    ILogger<SearchProductsQueryHandler> logger)
: IQueryHandler<SearchProductsQuery, SearchProductsResult>
{
    
    private static readonly TimeSpan AiTimeout = TimeSpan.FromMilliseconds(2000);
    
    public async Task<SearchProductsResult> HandleAsync(SearchProductsQuery query, CancellationToken ct)
    {
        if (query.UseAi)
        {
            try
            {
                return await aiGateway
                    .SearchAsync(query, ct)
                    .WaitAsync(AiTimeout, ct);
            }
            catch (TimeoutException)
            {
                logger.LogWarning(
                    "AI search timed out after {Ms}ms for query [{Query}]. " +
                    "Falling back to Elasticsearch.",
                    AiTimeout.TotalMilliseconds,
                    query.QueryText);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "AI search threw an exception for query [{Query}]. " +
                    "Falling back to Elasticsearch.",
                    query.QueryText);
            }
        }

        return await elasticsearchSearcher.SearchAsync(query, ct);
    }
}
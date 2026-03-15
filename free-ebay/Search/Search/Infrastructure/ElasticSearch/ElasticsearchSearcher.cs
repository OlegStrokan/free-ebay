using Application.Gateways;
using Infrastructure.ElasticSearch.Documents;
using Application.Queries.SearchProducts;

namespace Infrastructure.ElasticSearch;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;

public sealed class ElasticsearchSearcher : IElasticsearchSearcher
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchSearcher> _logger;

    public ElasticsearchSearcher(
        ElasticsearchClient client,
        ILogger<ElasticsearchSearcher> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<SearchProductsResult> SearchAsync(
        SearchProductsQuery query,
        CancellationToken   ct)
    {
        var from = (query.Page - 1) * query.Size;

        var response = await _client
            .SearchAsync<ProductSearchDocument>(s => s
                .Index(ElasticsearchIndexInitializer.IndexName)
                .From(from)
                .Size(query.Size)
                .Query(q => q
                    .MultiMatch(mm => mm
                        .Query(query.QueryText)
                        .Fields(new[]
                        {
                            "name^3",
                            "description",
                            "category^2",
                            "brand^2",
                            "color",
                            "layout"
                        })
                        .Type(TextQueryType.BestFields)
                    )
                ), ct);


        if (!response.IsValidResponse)
        {
            _logger.LogError(
                "Elasticsearch query failed for [{Query}]: {Debug}",
                query.QueryText,
                response.DebugInformation);

            return new SearchProductsResult(
                Items: [],
                TotalCount: 0,
                Page: query.Page,
                Size: query.Size,
                WasAiSearch: false,
                ParsedQueryDebug: null);
        }

        var items = response.Hits
            .Where(h => h.Source is not null)
            .Select(h => new ProductSearchItem(
                ProductId: Guid.Parse(h.Source!.Id),
                Name: h.Source.Name,
                Category: h.Source.Category,
                Price: (decimal)h.Source.Price,
                Currency: h.Source.Currency,
                RelevanceScore: h.Score ?? 0d,
                ImageUrls: h.Source.ImageUrls))
            .ToList();

        return new SearchProductsResult(
            Items: items,
            TotalCount: (int)response.Total,
            Page: query.Page,
            Size: query.Size,
            WasAiSearch: false,
            ParsedQueryDebug: null);
    }
}
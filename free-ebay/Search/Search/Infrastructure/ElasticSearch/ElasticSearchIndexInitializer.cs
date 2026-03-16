using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ElasticSearch;

// called once on startup, safe to call on every restart
public sealed class ElasticsearchIndexInitializer
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchIndexInitializer> _logger;

    public const string IndexName = "products";

    public ElasticsearchIndexInitializer(
        ElasticsearchClient client,
        ILogger<ElasticsearchIndexInitializer> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task EnsureIndexAsync(CancellationToken ct = default)
    {
        var exists = await _client.Indices.ExistsAsync(IndexName, ct);
        var indexExists = exists.ApiCallDetails.HttpStatusCode == 200;
        if (indexExists)
        {
            _logger.LogInformation(
                "Elasticsearch index [{Index}] already exists. Skipping.",
                IndexName);
            return;
        }

        _logger.LogInformation("Creating Elasticsearch index [{Index}].", IndexName);

        var response = await _client.Indices.CreateAsync(IndexName, c => c
            .Mappings(m => m
                .Properties(new Properties
                {
                    { "name", new TextProperty { Analyzer = "english" } },
                    { "description", new TextProperty { Analyzer = "english" } },
                    { "category", new KeywordProperty() },
                    { "price", new DoubleNumberProperty() },
                    { "currency", new KeywordProperty() },
                    { "stock", new IntegerNumberProperty() },
                    { "color", new KeywordProperty() },
                    { "layout", new KeywordProperty() },
                    { "brand", new KeywordProperty() },
                    { "image_urls", new KeywordProperty() },
                    { "indexed_at", new DateProperty() }
                })
            ), ct);

        if (!response.IsValidResponse)
        {
            _logger.LogError(
                "Failed to create Elasticsearch index [{Index}]: {Debug}",
                IndexName,
                response.DebugInformation);
        }
    }
}
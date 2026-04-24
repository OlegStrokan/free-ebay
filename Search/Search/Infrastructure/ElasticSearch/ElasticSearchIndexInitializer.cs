using Elastic.Clients.Elasticsearch;
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
                "Elasticsearch index [{Index}] already exists.",
                IndexName);
            return;
        }

        // The Catalog service owns the index schema and creates it on startup.
        // Search is read-only - do not create the index here.
        _logger.LogWarning(
            "Elasticsearch index [{Index}] does not exist. " +
            "The Catalog service must be running and have created the index before Search can serve queries.",
            IndexName);
    }
}
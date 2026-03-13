using Application.Models;
using Application.Services;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;

namespace Infrastructure.Elasticsearch;

public sealed class ElasticsearchIndexer(
    ElasticsearchClient client,
    IOptions<ElasticsearchOptions> options,
    ILogger<ElasticsearchIndexer> logger) : IElasticsearchIndexer
{
    private string IndexName => options.Value.IndexName;

    public async Task UpsertAsync(ProductSearchDocument document, CancellationToken ct = default)
    {
        var response = await client.IndexAsync(document, i => i
            .Index(IndexName)
            .Id(document.Id), ct);

        if (!response.IsValidResponse)
            logger.LogError(
                "Failed to upsert product {ProductId} into index '{Index}': {Error}",
                document.Id, IndexName, response.ElasticsearchServerError?.ToString());
    }

    public async Task UpdateFieldsAsync(
        string productId,
        ProductFieldsUpdateDocument update,
        CancellationToken ct = default)
    {
        var response = await client
            .UpdateAsync<ProductSearchDocument, ProductFieldsUpdateDocument>(
                productId,
                u => u.Index(IndexName).Doc(update),
                ct);

        if (!response.IsValidResponse)
            logger.LogError(
                "Failed to update product {ProductId} in index '{Index}': {Error}",
                productId, IndexName, response.ElasticsearchServerError?.ToString());
    }

    public async Task UpdateStockAsync(
        string productId,
        StockUpdateDocument update,
        CancellationToken ct = default)
    {
        var response = await client
            .UpdateAsync<ProductSearchDocument, StockUpdateDocument>(
                productId,
                u => u.Index(IndexName).Doc(update),
                ct);

        if (!response.IsValidResponse)
            logger.LogError(
                "Failed to update stock for product {ProductId} in index '{Index}': {Error}",
                productId, IndexName, response.ElasticsearchServerError?.ToString());
    }

    public async Task UpdateStatusAsync(
        string productId,
        StatusUpdateDocument update,
        CancellationToken ct = default)
    {
        var response = await client
            .UpdateAsync<ProductSearchDocument, StatusUpdateDocument>(
                productId,
                u => u.Index(IndexName).Doc(update),
                ct);

        if (!response.IsValidResponse)
            logger.LogError(
                "Failed to update status for product {ProductId} in index '{Index}': {Error}",
                productId, IndexName, response.ElasticsearchServerError?.ToString());
    }

    public async Task DeleteAsync(string productId, CancellationToken ct = default)
    {
        var response = await client.DeleteAsync(
            new DeleteRequest(IndexName, productId), ct);

        // NotFound is acceptable - delete is idempotent
        if (!response.IsValidResponse && response.Result != Elastic.Clients.Elasticsearch.Result.NotFound)
            logger.LogError(
                "Failed to delete product {ProductId} from index '{Index}': {Error}",
                productId, IndexName, response.ElasticsearchServerError?.ToString());
    }
}

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

        EnsureSucceeded(
            response.IsValidResponse,
            "upsert",
            document.Id,
            response.ElasticsearchServerError?.ToString());
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

        EnsureSucceeded(
            response.IsValidResponse,
            "update fields for",
            productId,
            response.ElasticsearchServerError?.ToString());
    }

    public async Task UpdateCatalogItemFieldsAsync(
        string catalogItemId,
        CatalogItemFieldsUpdateDocument update,
        CancellationToken ct = default)
    {
        var response = await client
            .UpdateAsync<ProductSearchDocument, CatalogItemFieldsUpdateDocument>(
                catalogItemId,
                u => u.Index(IndexName).Doc(update),
                ct);

        EnsureSucceeded(
            response.IsValidResponse,
            "update catalog item fields for",
            catalogItemId,
            response.ElasticsearchServerError?.ToString());
    }

    public async Task UpdateListingSummaryAsync(
        string catalogItemId,
        ListingSummaryUpdateDocument update,
        CancellationToken ct = default)
    {
        var response = await client
            .UpdateAsync<ProductSearchDocument, ListingSummaryUpdateDocument>(
                catalogItemId,
                u => u.Index(IndexName).Doc(update),
                ct);

        EnsureSucceeded(
            response.IsValidResponse,
            "update listing summary for",
            catalogItemId,
            response.ElasticsearchServerError?.ToString());
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

        EnsureSucceeded(
            response.IsValidResponse,
            "update stock for",
            productId,
            response.ElasticsearchServerError?.ToString());
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

        EnsureSucceeded(
            response.IsValidResponse,
            "update status for",
            productId,
            response.ElasticsearchServerError?.ToString());
    }

    public async Task DeleteAsync(string productId, CancellationToken ct = default)
    {
        var response = await client.DeleteAsync(
            new DeleteRequest(IndexName, productId), ct);

        // NotFound is acceptable - delete is idempotent
        EnsureSucceeded(
            response.IsValidResponse || response.Result == Elastic.Clients.Elasticsearch.Result.NotFound,
            "delete",
            productId,
            response.ElasticsearchServerError?.ToString());
    }

    private void EnsureSucceeded(bool succeeded, string operation, string productId, string? error)
    {
        if (succeeded)
            return;

        var message = $"Failed to {operation} product {productId} in index '{IndexName}': {error ?? "unknown Elasticsearch error"}";

        logger.LogError("{Message}", message);
        throw new ElasticsearchIndexingException(message);
    }
}

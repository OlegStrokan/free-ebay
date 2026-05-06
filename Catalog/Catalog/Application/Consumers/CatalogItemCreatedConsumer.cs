using System.Text.Json;
using Application.Events;
using Application.Models;
using Application.Services;
using Microsoft.Extensions.Logging;

namespace Application.Consumers;

public sealed class CatalogItemCreatedConsumer(
    IElasticsearchIndexer indexer,
    ILogger<CatalogItemCreatedConsumer> logger) : IProductEventConsumer
{
    public string EventType => "CatalogItemCreatedEvent";

    public async Task ConsumeAsync(JsonElement payload, CancellationToken ct)
    {
        var @event = JsonSerializer.Deserialize<CatalogItemCreatedEventPayload>(payload);
        if (@event is null)
        {
            logger.LogWarning("Failed to deserialize {EventType} payload", EventType);
            return;
        }

        var document = new ProductSearchDocument
        {
            Id = @event.CatalogItemId.Value.ToString(),
            Name = @event.Name,
            Description = @event.Description,
            CategoryId = @event.CategoryId.Value.ToString(),
            Price = 0m,
            Currency = "USD",
            Stock = 0,
            Attributes = @event.Attributes.ToDictionary(a => a.Key, a => a.Value),
            ImageUrls = @event.ImageUrls,
            Status = "OutOfStock",
            SellerId = string.Empty,
            CreatedAt = @event.CreatedAt,
            ProductType = "catalog_item",
            HasActiveListings = false,
            MinPrice = null,
            MinPriceCurrency = null,
            SellerCount = 0,
            BestCondition = null,
            TotalStock = 0,
        };

        await indexer.UpsertAsync(document, ct);

        logger.LogInformation(
            "Indexed new catalog item {CatalogItemId} ('{Name}') into Elasticsearch",
            document.Id, document.Name);
    }
}

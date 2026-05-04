using System.Text.Json;
using Application.Events;
using Application.Models;
using Application.Services;
using Microsoft.Extensions.Logging;

namespace Application.Consumers;

public sealed class CatalogItemUpdatedConsumer(
    IElasticsearchIndexer indexer,
    ILogger<CatalogItemUpdatedConsumer> logger) : IProductEventConsumer
{
    public string EventType => "CatalogItemUpdatedEvent";

    public async Task ConsumeAsync(JsonElement payload, CancellationToken ct)
    {
        var @event = JsonSerializer.Deserialize<CatalogItemUpdatedEventPayload>(payload);
        if (@event is null)
        {
            logger.LogWarning("Failed to deserialize {EventType} payload", EventType);
            return;
        }

        var catalogItemId = @event.CatalogItemId.Value.ToString();

        var update = new CatalogItemFieldsUpdateDocument
        {
            Name = @event.Name,
            Description = @event.Description,
            CategoryId = @event.CategoryId.Value.ToString(),
            Attributes = @event.Attributes.ToDictionary(a => a.Key, a => a.Value),
            ImageUrls = @event.ImageUrls,
            UpdatedAt = @event.UpdatedAt,
        };

        await indexer.UpdateCatalogItemFieldsAsync(catalogItemId, update, ct);

        logger.LogInformation(
            "Updated catalog item {CatalogItemId} fields in Elasticsearch",
            catalogItemId);
    }
}

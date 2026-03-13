using System.Text.Json;
using Application.Events;
using Application.Models;
using Application.Services;
using Microsoft.Extensions.Logging;

namespace Application.Consumers;

public sealed class ProductUpdatedConsumer(
    IElasticsearchIndexer indexer,
    ILogger<ProductUpdatedConsumer> logger) : IProductEventConsumer
{
    public string EventType => "ProductUpdatedEvent";

    public async Task ConsumeAsync(JsonElement payload, CancellationToken ct)
    {
        var @event = JsonSerializer.Deserialize<ProductUpdatedEventPayload>(payload);
        if (@event is null)
        {
            logger.LogWarning("Failed to deserialize {EventType} payload", EventType);
            return;
        }

        // Partial update preserves stock and status - only fields in the event are written
        var update = new ProductFieldsUpdateDocument
        {
            Name = @event.Name,
            Description = @event.Description,
            CategoryId = @event.CategoryId.Value.ToString(),
            Price = @event.Price.Amount,
            Currency = @event.Price.Currency,
            Attributes = @event.Attributes.ToDictionary(a => a.Key, a => a.Value),
            ImageUrls = @event.ImageUrls,
            UpdatedAt = @event.UpdatedAt,
        };

        await indexer.UpdateFieldsAsync(@event.ProductId.Value.ToString(), update, ct);

        logger.LogInformation(
            "Updated product {ProductId} fields in Elasticsearch",
            @event.ProductId.Value);
    }
}

using System.Text.Json;
using Application.Events;
using Application.Models;
using Application.Services;
using Microsoft.Extensions.Logging;

namespace Application.Consumers;

public sealed class ProductCreatedConsumer(
    IElasticsearchIndexer indexer,
    ILogger<ProductCreatedConsumer> logger) : IProductEventConsumer
{
    public string EventType => "ProductCreatedEvent";

    public async Task ConsumeAsync(JsonElement payload, CancellationToken ct)
    {
        var @event = JsonSerializer.Deserialize<ProductCreatedEventPayload>(payload);
        if (@event is null)
        {
            logger.LogWarning("Failed to deserialize {EventType} payload", EventType);
            return;
        }

        var document = new ProductSearchDocument
        {
            Id = @event.ProductId.Value.ToString(),
            Name = @event.Name,
            Description = @event.Description,
            CategoryId = @event.CategoryId.Value.ToString(),
            Price = @event.Price.Amount,
            Currency = @event.Price.Currency,
            Stock = @event.InitialStock,
            Attributes = @event.Attributes.ToDictionary(a => a.Key, a => a.Value),
            ImageUrls = @event.ImageUrls,
            Status = "Draft",
            SellerId = @event.SellerId.Value.ToString(),
            CreatedAt = @event.CreatedAt,
        };

        await indexer.UpsertAsync(document, ct);

        logger.LogInformation(
            "Indexed new product {ProductId} ('{Name}') into Elasticsearch",
            document.Id, document.Name);
    }
}

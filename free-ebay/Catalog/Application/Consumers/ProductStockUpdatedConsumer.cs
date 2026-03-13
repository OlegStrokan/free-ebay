using System.Text.Json;
using Application.Events;
using Application.Models;
using Application.Services;
using Microsoft.Extensions.Logging;

namespace Application.Consumers;

public sealed class ProductStockUpdatedConsumer(
    IElasticsearchIndexer indexer,
    ILogger<ProductStockUpdatedConsumer> logger) : IProductEventConsumer
{
    public string EventType => "ProductStockUpdatedEvent";

    public async Task ConsumeAsync(JsonElement payload, CancellationToken ct)
    {
        var @event = JsonSerializer.Deserialize<ProductStockUpdatedEventPayload>(payload);
        if (@event is null)
        {
            logger.LogWarning("Failed to deserialize {EventType} payload", EventType);
            return;
        }

        var productId = @event.ProductId.Value.ToString();
        var update = new StockUpdateDocument { Stock = @event.NewQuantity };

        await indexer.UpdateStockAsync(productId, update, ct);

        logger.LogInformation(
            "Updated stock for product {ProductId}: {Previous} → {New}",
            productId, @event.PreviousQuantity, @event.NewQuantity);
    }
}

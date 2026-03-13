using System.Text.Json;
using Application.Events;
using Application.Models;
using Application.Services;
using Microsoft.Extensions.Logging;

namespace Application.Consumers;

public sealed class ProductStatusChangedConsumer(
    IElasticsearchIndexer indexer,
    ILogger<ProductStatusChangedConsumer> logger) : IProductEventConsumer
{
    public string EventType => "ProductStatusChangedEvent";

    public async Task ConsumeAsync(JsonElement payload, CancellationToken ct)
    {
        var @event = JsonSerializer.Deserialize<ProductStatusChangedEventPayload>(payload);
        if (@event is null)
        {
            logger.LogWarning("Failed to deserialize {EventType} payload", EventType);
            return;
        }

        var productId = @event.ProductId.Value.ToString();
        var update = new StatusUpdateDocument { Status = @event.NewStatus };

        await indexer.UpdateStatusAsync(productId, update, ct);

        logger.LogInformation(
            "Updated status for product {ProductId}: {Previous} → {New}",
            productId, @event.PreviousStatus, @event.NewStatus);
    }
}

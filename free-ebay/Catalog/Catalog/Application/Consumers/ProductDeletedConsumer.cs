using System.Text.Json;
using Application.Events;
using Application.Services;
using Microsoft.Extensions.Logging;

namespace Application.Consumers;

public sealed class ProductDeletedConsumer(
    IElasticsearchIndexer indexer,
    ILogger<ProductDeletedConsumer> logger) : IProductEventConsumer
{
    public string EventType => "ProductDeletedEvent";

    public async Task ConsumeAsync(JsonElement payload, CancellationToken ct)
    {
        var @event = JsonSerializer.Deserialize<ProductDeletedEventPayload>(payload);
        if (@event is null)
        {
            logger.LogWarning("Failed to deserialize {EventType} payload", EventType);
            return;
        }

        var productId = @event.ProductId.Value.ToString();
        await indexer.DeleteAsync(productId, ct);

        logger.LogInformation("Removed product {ProductId} from Elasticsearch", productId);
    }
}

using System.Text.Json;
using Application.Events;
using Application.Models;
using Application.Services;
using Microsoft.Extensions.Logging;

namespace Application.Consumers;

public sealed class CatalogItemListingSummaryUpdatedConsumer(
    IElasticsearchIndexer indexer,
    ILogger<CatalogItemListingSummaryUpdatedConsumer> logger) : IProductEventConsumer
{
    public string EventType => "CatalogItemListingSummaryUpdatedEvent";

    public async Task ConsumeAsync(JsonElement payload, CancellationToken ct)
    {
        var @event = JsonSerializer.Deserialize<CatalogItemListingSummaryUpdatedEventPayload>(payload);
        if (@event is null)
        {
            logger.LogWarning("Failed to deserialize {EventType} payload", EventType);
            return;
        }

        var catalogItemId = @event.CatalogItemId.Value.ToString();

        var update = new ListingSummaryUpdateDocument
        {
            MinPrice = @event.MinPrice,
            MinPriceCurrency = @event.MinPriceCurrency,
            SellerCount = @event.SellerCount,
            HasActiveListings = @event.HasActiveListings,
            BestCondition = @event.BestCondition,
            TotalStock = @event.TotalStock,
            UpdatedAt = @event.UpdatedAt,
        };

        await indexer.UpdateListingSummaryAsync(catalogItemId, update, ct);

        logger.LogInformation(
            "Updated listing summary for catalog item {CatalogItemId}: sellers={SellerCount}, minPrice={MinPrice}, active={HasActive}",
            catalogItemId, @event.SellerCount, @event.MinPrice, @event.HasActiveListings);
    }
}

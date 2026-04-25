using System.Text.Json;
using Application.Commands.AdjustProductStock;
using Application.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Consumers;

public sealed class InventoryReleasedConsumer(
    ISender sender,
    ILogger<InventoryReleasedConsumer> logger) : IInventoryEventConsumer
{
    public string EventType => "InventoryReleased";

    public async Task ConsumeAsync(JsonElement payload, CancellationToken ct)
    {
        var @event = JsonSerializer.Deserialize<InventoryReservationEventPayload>(
            payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (@event is null)
        {
            logger.LogWarning("Failed to deserialize {EventType} payload", EventType);
            return;
        }

        foreach (var item in @event.Items)
        {
            if (!Guid.TryParse(item.ProductId, out var productId))
            {
                logger.LogWarning(
                    "Invalid productId '{ProductId}' in {EventType} for reservation {ReservationId} — skipping item",
                    item.ProductId, EventType, @event.ReservationId);
                continue;
            }

            var result = await sender.Send(new AdjustProductStockCommand(productId, +item.Quantity), ct);

            if (!result.IsSuccess)
                logger.LogWarning(
                    "AdjustStock(+{Qty}) failed for product {ProductId} in reservation {ReservationId}: {Errors}",
                    item.Quantity, productId, @event.ReservationId, string.Join(", ", result.Errors));
            else
                logger.LogInformation(
                    "Stock restored by {Qty} for product {ProductId} (reservation {ReservationId} released)",
                    item.Quantity, productId, @event.ReservationId);
        }
    }
}

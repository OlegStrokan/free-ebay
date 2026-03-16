using Application.Interfaces;
using Application.Models;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public sealed class InventoryService(
    IInventoryReservationStore store,
    ILogger<InventoryService> logger) : IInventoryService
{
    public async Task<ReserveInventoryResult> ReserveAsync(
        ReserveInventoryCommand command,
        CancellationToken cancellationToken)
    {
        if (command.OrderId == Guid.Empty)
        {
            return ReserveInventoryResult.Failed(
                "OrderId must be a valid GUID.",
                ReserveInventoryFailureReason.Validation);
        }

        if (command.Items.Count == 0)
        {
            return ReserveInventoryResult.Failed(
                "At least one inventory item is required.",
                ReserveInventoryFailureReason.Validation);
        }

        List<ReserveStockItem> normalizedItems;

        try
        {
            normalizedItems = NormalizeItems(command.Items);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Inventory reservation input validation failed");
            return ReserveInventoryResult.Failed(ex.Message, ReserveInventoryFailureReason.Validation);
        }

        return await store.ReserveAsync(command.OrderId, normalizedItems, cancellationToken);
    }

    public async Task<ReleaseInventoryResult> ReleaseAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        if (reservationId == Guid.Empty)
            return ReleaseInventoryResult.Failed("ReservationId must be a valid GUID");

        return await store.ReleaseAsync(reservationId, cancellationToken);
    }

    private static List<ReserveStockItem> NormalizeItems(
        IReadOnlyCollection<ReserveInventoryItemInput> items)
    {
        var grouped = items
            .GroupBy(x => x.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Quantity = g.Sum(x => x.Quantity)
            })
            .ToList();

        var normalized = new List<ReserveStockItem>(grouped.Count);

        foreach (var item in grouped)
        {
            if (item.ProductId == Guid.Empty)
                throw new ArgumentException("Every product_id must be a valid GUID", nameof(items));

            if (item.Quantity <= 0)
                throw new ArgumentException("Every item quantity must be greater than zero", nameof(items));

            normalized.Add(new ReserveStockItem(item.ProductId, item.Quantity));
        }

        return normalized;
    }
}

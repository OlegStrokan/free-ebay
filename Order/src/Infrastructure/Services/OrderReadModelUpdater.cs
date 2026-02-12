using System.Text.Json;
using Domain.Events.CreateOrder;
using Infrastructure.Persistence.DbContext;
using Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

// keeps the read model in sync with the event store
// can be triggered by: outbox processor, background service, in-memory event bus (immediate consistency)
public class OrderReadModelUpdater(AppDbContext dbContext, ILogger<OrderReadModelUpdater> logger)
{
    public async Task HandleAsync(OrderCreatedEvent evt, CancellationToken ct = default)
    {
        var itemsJson = JsonSerializer.Serialize(evt.Items.Select(item =>
            new
            {
                ProductId = item.ProductId.Value,
                Quantity = item.Quantity,
                Price = item.PriceAtPurchase.Amount,
                Currency = item.PriceAtPurchase.Currency
            }));

        var readModel = new OrderReadModel
        {
            Id = evt.OrderId.Value,
            CustomerId = evt.CustomerId.Value,
            Status = "Pending",
            TotalAmount = evt.TotalPrice.Amount,
            Currency = evt.TotalPrice.Currency,
            DeliveryStreet = evt.DeliveryAddress.Street,
            DeliveryCity = evt.DeliveryAddress.City,
            DeliveryPostalCode = evt.DeliveryAddress.PostalCode,
            ItemsJson = itemsJson,
            CreatedAt = evt.CreatedAt,
            Version = 0,
            LastSyncedAt = DateTime.UtcNow
        };

        dbContext.OrderReadModels.Add(readModel);
        await dbContext.SaveChangesAsync(ct);
        
        logger.LogInformation(
            "Created OrderReadModel for Order {OrderId}",
            evt.OrderId.Value);
    }

    public async Task HandleAsync(OrderPaidEvent evt, CancellationToken ct = default)
    {
        var readModel = await GetReadModelAsync(evt.OrderId.Value, ct);
        if (readModel == null) return;

        readModel.Status = "Paid";
        // @todo: update paymentId.Value type. should it be string or Guid?
        readModel.PaymentId = evt.PaymentId.Value;
        readModel.UpdatedAt = evt.PaidAt;
        readModel.Version++;
        readModel.LastSyncedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);
        
        logger.LogInformation(
            "Updated OrderReadModel fro Order {OrderId} - Status: Paid",
            evt.OrderId.Value);
    }

    public async Task HandleAsync(OrderTrackingAssignedEvent evt, CancellationToken ct = default)
    {
        var readModel = await GetReadModelAsync(evt.OrderId.Value, ct);
        if (readModel == null) return;

        readModel.TrackingId = evt.TrackingId.Value;
        readModel.UpdatedAt = evt.AssignedAt;
        readModel.Version++;
        readModel.LastSyncedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "Update OrderReadModel for Order {OrderId} - Tracking assigned",
            evt.OrderId.Value);
    }

    private async Task HandleAsync(OrderCompletedEvent evt, CancellationToken ct = default)
    {
        var readModel = await GetReadModelAsync(evt.OrderId.Value, ct);
        if (readModel == null) return;

        readModel.Status = "Completed";
        readModel.CompletedAt = evt.CompletedAt;
        readModel.UpdatedAt = evt.CompletedAt;
        readModel.Version++;

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Update OrderReadModel for Order {OrderId} - Status: Completed",
            evt.OrderId.Value);
    }


    private async Task HandleAsync(OrderCancelledEvent evt, CancellationToken ct = default)
    {
        var readModel = await GetReadModelAsync(evt.OrderId.Value, ct);
        if (readModel == null) return;

        readModel.Status = "Completed";
        readModel.UpdatedAt = evt.CancelledAt;
        readModel.LastSyncedAt = DateTime.UtcNow,
        readModel.Version++;

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Update OrderReadModel for Order {OrderId} - Status: Cancelled",
            evt.OrderId.Value);
    }
    

    private async Task<OrderReadModel?> GetReadModelAsync(Guid orderId, CancellationToken ct)
    {
        var readModel = await dbContext.OrderReadModels
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (readModel == null)
        {
            logger.LogWarning(
                "OrderReadModel not found for Order {OrderId}. May need to rebuild from events.",
                orderId);
        }

        return readModel;
    }
}
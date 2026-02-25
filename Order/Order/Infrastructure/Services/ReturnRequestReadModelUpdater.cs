using System.Text.Json;
using Domain.Events.OrderReturn;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;
using Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ReturnRequestReadModelUpdater(AppDbContext dbContext, ILogger<ReturnRequestReadModelUpdater> logger)
{
    public async Task HandleAsync(ReturnRequestCreatedEvent evt, CancellationToken ct = default)
    {
        var exists = await dbContext.ReturnRequestReadModels
            .AnyAsync(r => r.Id == evt.ReturnRequestId.Value, ct);

        if (exists)
        {
            logger.LogInformation(
                "ReturnRequest {ReturnRequestId} already exists in read model. Skipping duplicate event.",
                evt.ReturnRequestId.Value);
            return;
        }

        var itemsJson = JsonSerializer.Serialize(evt.ItemsToReturn.Select(item =>
            new
            {
                ProductId = item.ProductId.Value,
                Quantity = item.Quantity,
                Price = item.PriceAtPurchase.Amount,
                Currency = item.PriceAtPurchase.Currency
            })
        );

        var readModels = new ReturnRequestReadModel
        {
            Id = evt.ReturnRequestId.Value,
            OrderId = evt.OrderId.Value,
            CustomerId = evt.CustomerId.Value,
            Status = "Pending",
            Reason = evt.Reason,
            RefundAmount = evt.RefundAmount.Amount,
            Currency = evt.RefundAmount.Currency,
            ItemsToReturnJson = itemsJson,
            RequestedAt = evt.RequestedAt,
            Version = 0,
            LastSyncedAt = DateTime.UtcNow
        };

        dbContext.ReturnRequestReadModels.Add(readModels);

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Created ReturnRequestReadModel for ReturnRequest {ReturnRequestId}",
            evt.ReturnRequestId.Value);
    }

    public async Task HandleAsync(
        ReturnItemsReceivedEvent evt,
        CancellationToken ct = default)
    {
        var readModel = await GetReadModelAsync(evt.ReturnRequestId.Value, ct);
        if (readModel == null) return;

        readModel.Status = "ItemsReceived";
        readModel.UpdatedAt = evt.ReceivedAt;
        readModel.Version++;
        readModel.LastSyncedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "Updated ReturnRequestReadModel for ReturnRequest {ReturnRequestId} - Status: ItemReceived",
            evt.ReturnRequestId.Value);
    }
    
    

    public async Task HandleAsync(
        ReturnRefundProcessedEvent evt,
        CancellationToken ct = default)
    {
        var readModel = await GetReadModelAsync(evt.ReturnRequestId.Value, ct);
        if (readModel == null) return;

        readModel.Status = "RefundProcessed";
        readModel.UpdatedAt = evt.RefundedAt;
        readModel.Version++;
        readModel.LastSyncedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "Update ReturnRequestReadModel for ReturnRequest {ReturnRequestId} - Status: RefundProcessed",
            evt.ReturnRequestId.Value);
    }

    public async Task HandleAsync(
        ReturnCompletedEvent evt,
        CancellationToken ct = default)
    {
        var readModel = await GetReadModelAsync(evt.ReturnRequestId.Value, ct);
        if (readModel == null) return;

        readModel.Status = "Completed";
        readModel.CompletedAt = evt.CompletedAt;
        readModel.UpdatedAt = evt.CompletedAt;
        readModel.Version++;
        readModel.LastSyncedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);
        
        logger.LogInformation(
            "Update ReturnRequestReadModel for ReturnRequest {ReturnRequestId} - Status: Completed",
            evt.ReturnRequestId.Value);
    }

    private async Task<ReturnRequestReadModel?> GetReadModelAsync(Guid returnRequestId, CancellationToken ct)
    {
        var readModel = await dbContext.ReturnRequestReadModels
            .FirstOrDefaultAsync(r => r.Id == returnRequestId, ct);

        if (readModel == null)
        {
            logger.LogWarning(
                "ReturnRequestReadModel not found for ReturnRequest {ReturnRequestId}. " +
                "May need to rebuild from events.",
                returnRequestId);
        }

        return readModel;
    }
    
}
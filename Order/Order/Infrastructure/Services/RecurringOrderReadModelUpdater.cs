using System.Text.Json;
using Domain.Events.Subscription;
using Infrastructure.Persistence.DbContext;
using Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class RecurringOrderReadModelUpdater(
    ReadDbContext dbContext,
    ILogger<RecurringOrderReadModelUpdater> logger)
    : IReadModelUpdater
{
    private static readonly HashSet<Type> _handledTypes =
    [
        typeof(RecurringOrderCreatedEvent),
        typeof(RecurringOrderPausedEvent),
        typeof(RecurringOrderResumedEvent),
        typeof(RecurringOrderCancelledEvent),
        typeof(RecurringOrderExecutedEvent),
        typeof(RecurringOrderExecutionFailedEvent)
    ];

    public bool CanHandle(Type eventType) => _handledTypes.Contains(eventType);

    public async Task HandleAsync(RecurringOrderCreatedEvent evt, CancellationToken ct = default)
    {
        var exists = await dbContext.RecurringOrderReadModels
            .AnyAsync(r => r.Id == evt.RecurringOrderId.Value, ct);

        if (exists)
        {
            logger.LogInformation(
                "RecurringOrderReadModel {Id} already exists. Skipping duplicate.", evt.RecurringOrderId.Value);
            return;
        }

        var items = evt.Items.Select(i => new RecurringItemRecord(
            ProductId: i.ProductId.Value,
            Quantity: i.Quantity,
            Price: i.Price.Amount,
            Currency: i.Price.Currency)).ToList();

        var readModel = new RecurringOrderReadModel
        {
            Id = evt.RecurringOrderId.Value,
            CustomerId = evt.CustomerId.Value,
            PaymentMethod = evt.PaymentMethod,
            Frequency = evt.Frequency,
            Status = "Active",
            NextRunAt = evt.NextRunAt,
            LastRunAt = null,
            TotalExecutions = 0,
            MaxExecutions = evt.MaxExecutions,
            DeliveryStreet = evt.DeliveryAddress.Street,
            DeliveryCity = evt.DeliveryAddress.City,
            DeliveryCountry = evt.DeliveryAddress.Country,
            DeliveryPostalCode = evt.DeliveryAddress.PostalCode,
            ItemsJson = JsonSerializer.Serialize(items),
            CreatedAt = evt.CreatedAt,
            UpdatedAt = null,
            Version = 0,
            LastSyncedAt = DateTime.UtcNow
        };

        dbContext.RecurringOrderReadModels.Add(readModel);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Created RecurringOrderReadModel for {Id}", evt.RecurringOrderId.Value);
    }

    public async Task HandleAsync(RecurringOrderPausedEvent evt, CancellationToken ct = default)
    {
        var model = await GetReadModelAsync(evt.RecurringOrderId.Value, ct);
        if (model is null) return;

        model.Status = "Paused";
        model.UpdatedAt = evt.PausedAt;
        model.Version++;
        model.LastSyncedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task HandleAsync(RecurringOrderResumedEvent evt, CancellationToken ct = default)
    {
        var model = await GetReadModelAsync(evt.RecurringOrderId.Value, ct);
        if (model is null) return;

        model.Status = "Active";
        model.NextRunAt = evt.NextRunAt;
        model.UpdatedAt = evt.ResumedAt;
        model.Version++;
        model.LastSyncedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task HandleAsync(RecurringOrderCancelledEvent evt, CancellationToken ct = default)
    {
        var model = await GetReadModelAsync(evt.RecurringOrderId.Value, ct);
        if (model is null) return;

        model.Status = "Cancelled";
        model.UpdatedAt = evt.CancelledAt;
        model.Version++;
        model.LastSyncedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task HandleAsync(RecurringOrderExecutedEvent evt, CancellationToken ct = default)
    {
        var model = await GetReadModelAsync(evt.RecurringOrderId.Value, ct);
        if (model is null) return;

        model.TotalExecutions = evt.ExecutionNumber;
        model.LastRunAt = evt.ExecutedAt;
        model.NextRunAt = evt.NextRunAt;
        model.UpdatedAt = evt.ExecutedAt;
        model.Version++;
        model.LastSyncedAt = DateTime.UtcNow;

        // if MaxExecutions reached, auto-cancel read model
        if (model.MaxExecutions.HasValue && model.TotalExecutions >= model.MaxExecutions.Value)
            model.Status = "Cancelled";

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task HandleAsync(RecurringOrderExecutionFailedEvent evt, CancellationToken ct = default)
    {
        var model = await GetReadModelAsync(evt.RecurringOrderId.Value, ct);
        if (model is null) return;

        model.NextRunAt = evt.NextRetryAt;
        model.UpdatedAt = evt.FailedAt;
        model.Version++;
        model.LastSyncedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);
    }
    
    public async Task HandleAsync(object domainEvent, CancellationToken ct = default)
    {
        await (domainEvent switch
        {
            RecurringOrderCreatedEvent e => HandleAsync(e, ct),
            RecurringOrderPausedEvent e => HandleAsync(e, ct),
            RecurringOrderResumedEvent e => HandleAsync(e, ct),
            RecurringOrderCancelledEvent e => HandleAsync(e, ct),
            RecurringOrderExecutedEvent e => HandleAsync(e, ct),
            RecurringOrderExecutionFailedEvent e => HandleAsync(e, ct),
            _ => Task.CompletedTask
        });
    }
    
    private async Task<RecurringOrderReadModel?> GetReadModelAsync(Guid id, CancellationToken ct)
    {
        var model = await dbContext.RecurringOrderReadModels
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (model is null)
            logger.LogWarning(
                "RecurringOrderReadModel not found for {Id}. May need to rebuild from events.", id);

        return model;
    }

    private sealed record RecurringItemRecord(
        Guid ProductId,
        int Quantity,
        decimal Price,
        string Currency);
}

using System.Data;
using System.Text.Json;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Entities.Order;
using Domain.Entities.Subscription;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class RecurringOrderPersistenceService(
    IEventStoreRepository eventStore,
    IOutboxRepository outboxRepository,
    IIdempotencyRepository idempotencyRepository,
    ISnapshotRepository snapshotRepository,
    AppDbContext dbContext,
    ILogger<RecurringOrderPersistenceService> logger)
    : IRecurringOrderPersistenceService
{
    private const int SnapshotThreshold = 50;
    
    public async Task CreateAsync(RecurringOrder order, string idempotencyKey, CancellationToken ct)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                await eventStore.SaveEventsAsync(
                    order.Id.Value.ToString(),
                    AggregateTypes.RecurringOrder,
                    order.UncommitedEvents,
                    expectedVersion: -1,
                    ct);

                foreach (var evt in order.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        evt.EventId,
                        evt.GetType().Name,
                        JsonSerializer.Serialize(evt, evt.GetType()),
                        evt.OccurredOn,
                        order.Id.Value.ToString(),
                        ct);
                }

                await idempotencyRepository.SaveAsync(idempotencyKey, order.Id.Value, DateTime.UtcNow, ct);
                await dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                order.ClearUncommittedEvents();

                logger.LogInformation(
                    "Created RecurringOrder {Id} for customer {CustomerId} — next run: {NextRunAt}",
                    order.Id.Value, order.CustomerId.Value, order.NextRunAt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed creating RecurringOrder");
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }
    
    public async Task UpdateAsync(
        Guid recurringOrderId,
        Func<RecurringOrder, Task> action,
        CancellationToken ct)
    {
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await TryUpdateAsync(recurringOrderId, action, ct);
                return;
            }
            catch (ConcurrencyConflictException) when (attempt < maxRetries)
            {
                logger.LogWarning(
                    "Concurrency conflict on attempt {Attempt}/{Max} for RecurringOrder {Id}. Retrying...",
                    attempt, maxRetries, recurringOrderId);
            }
        }
        throw new ConcurrencyConflictException(
            AggregateTypes.RecurringOrder, recurringOrderId.ToString(), maxRetries);
    }

    public async Task<Guid> ExecuteAsync(Guid recurringOrderId, CancellationToken ct)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        Guid createdOrderId = Guid.Empty;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                var recurring = await LoadAsync(recurringOrderId, ct)
                    ?? throw new DomainException($"RecurringOrder {recurringOrderId} not found");

                if (!recurring.IsDue)
                    throw new DomainException(
                        $"RecurringOrder {recurringOrderId} is not Active or not yet due");

                var expectedVersion = recurring.Version - 1;

                var orderItems = recurring.Items.Select(i =>
                    OrderItem.Create(i.ProductId, i.Quantity, i.Price)).ToList();

                var childOrder = Order.Create(
                    recurring.CustomerId,
                    recurring.DeliveryAddress,
                    orderItems);

                createdOrderId = childOrder.Id.Value;

                await eventStore.SaveEventsAsync(
                    childOrder.Id.Value.ToString(),
                    AggregateTypes.Order,
                    childOrder.UncommitedEvents,
                    expectedVersion: -1,
                    ct);

                foreach (var evt in childOrder.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        evt.EventId,
                        evt.GetType().Name,
                        JsonSerializer.Serialize(evt, evt.GetType()),
                        evt.OccurredOn,
                        childOrder.Id.Value.ToString(),
                        ct);
                }

                recurring.RecordExecution(createdOrderId);

                await eventStore.SaveEventsAsync(
                    recurring.Id.Value.ToString(),
                    AggregateTypes.RecurringOrder,
                    recurring.UncommitedEvents,
                    expectedVersion,
                    ct);

                foreach (var evt in recurring.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        evt.EventId,
                        evt.GetType().Name,
                        JsonSerializer.Serialize(evt, evt.GetType()),
                        evt.OccurredOn,
                        recurring.Id.Value.ToString(),
                        ct);
                }

                await dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                childOrder.ClearUncommittedEvents();
                recurring.ClearUncommittedEvents();

                logger.LogInformation(
                    "RecurringOrder {RecurringId} executed → Order {OrderId} (execution #{N})",
                    recurringOrderId, createdOrderId, recurring.TotalExecutions);

                if (recurring.Version > 0 && recurring.Version % SnapshotThreshold == 0)
                    await TakeSnapshotSafeAsync(recurring, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed executing RecurringOrder {Id}", recurringOrderId);
                await transaction.RollbackAsync(ct);
                throw;
            }
        });

        return createdOrderId;
    }
    
    public async Task<RecurringOrder?> LoadAsync(Guid recurringOrderId, CancellationToken ct)
    {
        var snapshot = await snapshotRepository.GetLatestAsync(
            recurringOrderId.ToString(), AggregateTypes.RecurringOrder, ct);

        if (snapshot is null)
            return await ReplayFromStoreAsync(recurringOrderId, ct);

        var state = JsonSerializer.Deserialize<RecurringOrderSnapshotState>(snapshot.StateJson);
        if (state is null)
        {
            logger.LogWarning(
                "Failed to deserialize RecurringOrder snapshot {Id}. Falling back to full replay.",
                recurringOrderId);
            return await ReplayFromStoreAsync(recurringOrderId, ct);
        }

        var order = RecurringOrder.FromSnapshot(state);

        var delta = await eventStore.GetEventsAfterVersionAsync(
            recurringOrderId.ToString(), AggregateTypes.RecurringOrder, snapshot.Version, ct);

        order.LoadFromHistory(delta);
        return order;
    }
    
    private async Task TryUpdateAsync(
        Guid recurringOrderId,
        Func<RecurringOrder, Task> action,
        CancellationToken ct)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                var order = await LoadAsync(recurringOrderId, ct)
                    ?? throw new DomainException($"RecurringOrder {recurringOrderId} not found");

                var expectedVersion = order.Version - 1;
                await action(order);

                if (!order.UncommitedEvents.Any())
                {
                    await transaction.RollbackAsync(ct);
                    return;
                }

                await eventStore.SaveEventsAsync(
                    order.Id.Value.ToString(),
                    AggregateTypes.RecurringOrder,
                    order.UncommitedEvents,
                    expectedVersion,
                    ct);

                foreach (var evt in order.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        evt.EventId,
                        evt.GetType().Name,
                        JsonSerializer.Serialize(evt, evt.GetType()),
                        evt.OccurredOn,
                        order.Id.Value.ToString(),
                        ct);
                }

                await dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                order.ClearUncommittedEvents();

                if (order.Version > 0 && order.Version % SnapshotThreshold == 0)
                    await TakeSnapshotSafeAsync(order, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed updating RecurringOrder {Id}", recurringOrderId);
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }

    private async Task<RecurringOrder?> ReplayFromStoreAsync(Guid recurringOrderId, CancellationToken ct)
    {
        var events = await eventStore.GetEventsAsync(
            recurringOrderId.ToString(), AggregateTypes.RecurringOrder, ct);

        if (!events.Any()) return null;
        return RecurringOrder.FromHistory(events);
    }

    private async Task TakeSnapshotSafeAsync(RecurringOrder order, CancellationToken ct)
    {
        try
        {
            var snapshot = AggregateSnapshot.Create(
                order.Id.Value.ToString(),
                AggregateTypes.RecurringOrder,
                order.Version - 1, // 0-indexed last committed event version
                JsonSerializer.Serialize(order.ToSnapshotState()));

            await snapshotRepository.SaveAsync(snapshot, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to take snapshot for RecurringOrder {Id} at version {Version}",
                order.Id.Value, order.Version);
        }
    }
}

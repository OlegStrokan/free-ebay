using System.Data;
using System.Text.Json;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Entities.B2BOrder;
using Domain.Entities.Order;
using Domain.Exceptions;
using Domain.Interfaces;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class B2BOrderPersistenceService(
    IEventStoreRepository eventStore,
    IOutboxRepository outboxRepository,
    IIdempotencyRepository idempotencyRepository,
    ISnapshotRepository snapshotRepository,
    AppDbContext dbContext,
    ILogger<B2BOrderPersistenceService> logger) : IB2BOrderPersistenceService
{
    // @think: debatable, maybe should be increased to 50
    private const int SnapshotThreshold = 20;
    
    public async Task StartB2BOrderAsync(
        B2BOrder order,
        string idempotencyKey,
        CancellationToken ct)
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
                    AggregateTypes.B2BOrder,
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
                    "Started B2BOrder {B2BOrderId} for company '{Company}'",
                    order.Id.Value, order.CompanyName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed starting B2BOrder");
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }
    
    public async Task UpdateB2BOrderAsync(
        Guid b2bOrderId,
        Func<B2BOrder, Task> action,
        CancellationToken ct)
    {
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await TryUpdateAsync(b2bOrderId, action, ct);
                return;
            }
            catch (ConcurrencyConflictException) when (attempt < maxRetries)
            {
                logger.LogWarning(
                    "Concurrency conflict on attempt {Attempt}/{Max} for B2BOrder {Id}. Retrying...",
                    attempt, maxRetries, b2bOrderId);
            }
        }

        throw new ConcurrencyConflictException(AggregateTypes.B2BOrder, b2bOrderId.ToString(), maxRetries);
    }

    private async Task TryUpdateAsync(Guid b2bOrderId, Func<B2BOrder, Task> action, CancellationToken ct)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                var order = await LoadB2BOrderAsync(b2bOrderId, ct)
                    ?? throw new DomainException($"B2BOrder {b2bOrderId} not found");

                // @todo: make sure what we have consistency between all tests
                // Version is 1-indexed (count of applied events); DB stores events 0-indexed
                // (first event at version 0), so the last committed version = Version - 1 type shit
                var expectedVersion = order.Version - 1;

                await action(order);

                await eventStore.SaveEventsAsync(
                    order.Id.Value.ToString(),
                    AggregateTypes.B2BOrder,
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
                {
                    await TakeSnapshotSafeAsync(order, ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed updating B2BOrder {B2BOrderId}", b2bOrderId);
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }
    
    public async Task FinalizeB2BOrderAsync(
        Guid b2bOrderId,
        Order orderToCreate,
        string idempotencyKey,
        CancellationToken ct)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            try
            {
                // idempotency guard: if this key was already processed, skip silently
                var existing = await idempotencyRepository.GetByKeyAsync(idempotencyKey, ct);
                if (existing is not null)
                {
                    await transaction.CommitAsync(ct);
                    return;
                }

                var b2bOrder = await LoadB2BOrderAsync(b2bOrderId, ct)
                    ?? throw new DomainException($"B2BOrder {b2bOrderId} not found");

                // Capture expected version before mutating the aggregate (same 0-indexed offset).
                var b2bOrderExpectedVersion = b2bOrder.Version - 1;

                b2bOrder.Finalize(orderToCreate.Id.Value);

                await eventStore.SaveEventsAsync(
                    orderToCreate.Id.Value.ToString(),
                    AggregateTypes.Order,
                    orderToCreate.UncommitedEvents,
                    expectedVersion: -1,
                    ct);

                await eventStore.SaveEventsAsync(
                    b2bOrder.Id.Value.ToString(),
                    AggregateTypes.B2BOrder,
                    b2bOrder.UncommitedEvents,
                    b2bOrderExpectedVersion,
                    ct);

                foreach (var evt in orderToCreate.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        evt.EventId,
                        evt.GetType().Name,
                        JsonSerializer.Serialize(evt, evt.GetType()),
                        evt.OccurredOn,
                        orderToCreate.Id.Value.ToString(),
                        ct);
                }

                foreach (var evt in b2bOrder.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        evt.EventId,
                        evt.GetType().Name,
                        JsonSerializer.Serialize(evt, evt.GetType()),
                        evt.OccurredOn,
                        b2bOrder.Id.Value.ToString(),
                        ct);
                }

                await idempotencyRepository.SaveAsync(
                    idempotencyKey,
                    orderToCreate.Id.Value,
                    DateTime.UtcNow,
                    ct);

                orderToCreate.ClearUncommittedEvents();
                b2bOrder.ClearUncommittedEvents();

                await dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                logger.LogInformation(
                    "Finalized B2BOrder {B2BOrderId} → Order {OrderId}",
                    b2bOrderId, orderToCreate.Id.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed finalizing B2BOrder {B2BOrderId}", b2bOrderId);
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }
    
    public async Task<B2BOrder?> LoadB2BOrderAsync(Guid b2bOrderId, CancellationToken ct)
    {
        var snapshot = await snapshotRepository.GetLatestAsync(
            b2bOrderId.ToString(), AggregateTypes.B2BOrder, ct);

        if (snapshot is null)
            return await ReplayFromStoreAsync(b2bOrderId, ct);

        var state = JsonSerializer.Deserialize<B2BOrderSnapshotState>(snapshot.StateJson);
        if (state is null)
        {
            logger.LogWarning(
                "Failed to deserialize B2BOrder snapshot {B2BOrderId}. Falling back to full replay.",
                b2bOrderId);
            return await ReplayFromStoreAsync(b2bOrderId, ct);
        }

        var order = B2BOrder.FromSnapshot(state);

        var delta = await eventStore.GetEventsAfterVersionAsync(
            b2bOrderId.ToString(), AggregateTypes.B2BOrder, snapshot.Version, ct);

        order.LoadFromHistory(delta);

        logger.LogDebug(
            "Loaded B2BOrder {B2BOrderId}: snapshot v{SnapshotVersion} + {DeltaCount} delta events",
            b2bOrderId, snapshot.Version, delta.Count());

        return order;
    }
    
    private async Task<B2BOrder?> ReplayFromStoreAsync(Guid b2bOrderId, CancellationToken ct)
    {
        var events = await eventStore.GetEventsAsync(
            b2bOrderId.ToString(), AggregateTypes.B2BOrder, ct);

        if (!events.Any()) return null;

        return B2BOrder.FromHistory(events);
    }

    private async Task TakeSnapshotSafeAsync(B2BOrder order, CancellationToken ct)
    {
        try
        {
            var state = order.ToSnapshotState();
            var snapshot = AggregateSnapshot.Create(
                order.Id.Value.ToString(),
                AggregateTypes.B2BOrder,
                order.Version - 1, // store last committed event version (0-indexed)
                JsonSerializer.Serialize(state));

            await snapshotRepository.SaveAsync(snapshot, ct);

            logger.LogInformation(
                "Took snapshot for B2BOrder {B2BOrderId} at version {Version}",
                order.Id.Value, order.Version);
        }
        catch (Exception ex)
        {
            // snapshot failure is never fatal - we can always replay
            logger.LogWarning(ex,
                "Failed to take snapshot for B2BOrder {B2BOrderId} at version {Version}. " +
                "Will replay from full event history on next load.",
                order.Id.Value, order.Version);
        }
    }
}

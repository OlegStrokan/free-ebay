using System.Data;
using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Entities.Order;
using Domain.Exceptions;
using Domain.Interfaces;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class OrderPersistenceService(
    IEventStoreRepository eventStore,
    IOutboxRepository outboxRepository,
    IIdempotencyRepository idempotencyRepository,
    ISnapshotRepository  snapshotRepository,
    AppDbContext dbContext,
    ILogger<OrderPersistenceService> logger) : IOrderPersistenceService
{
    private const int SnapshotThreshold = 50;

    
    // we use optimistic locking to prevent.....locking.
    // more details you can find in integration tests
    public async Task UpdateOrderAsync(
        Guid orderId,
        Func<Order, Task> action,
        CancellationToken cancellationToken)
    {
        const int maxRetries = 3;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await TryUpdateOrderAsync(orderId, action, cancellationToken);
                return;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict") && attempt < maxRetries)
            {
                logger.LogWarning(
                    "Concurrency conflict on attempt {Attempt}/{MaxRetries} for Order {OrderId}. Reloading and retrying...",
                    attempt, maxRetries, orderId);
            }
        }
    }

    private async Task TryUpdateOrderAsync(
        Guid orderId,
        Func<Order, Task> action,
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            try
            {
                var order = await LoadOrderAsync(orderId, cancellationToken);

                if (order is null) throw new OrderNotFoundException(orderId);

                var expectedVersion = order.Version;
                
                /* 
                 * you can argue what we can't retry "action" here: UpdateOrderAsync, BUT:
                 * action mutates the aggregate which is fine because the aggregate
                 * reloaded fresh each retry. But if action ever does some external type shit
                 * like call api, or fucks without condom - unpredictable stuff will happen. ok?
                 */
                await action(order);

                /*
                NOTE:
                The only thing to watch: if after 3 attempts it still fails, the exception propagates 
                as-is (InvalidOperationException with "Concurrency conflict"). You may
                 want to wrap it in a domain-specific exception like OrderConcurrencyException so callers 
                 can distinguish it from other InvalidOperationExceptions rather than relying on string matching.

                */

                await eventStore.SaveEventsAsync(
                    order.Id.Value.ToString(),
                    "Order",
                    order.UncommitedEvents,
                    expectedVersion,
                    cancellationToken);
                
                foreach (var domainEvent in order.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        domainEvent.EventId, // helps to detect duplicates 
                        domainEvent.GetType().Name,
                        JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                        DateTime.UtcNow,
                        order.Id.Value.ToString(),
                        cancellationToken);
                }

                order.ClearUncommittedEvents();

                await dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                if (order.Version > 0 && order.Version % SnapshotThreshold == 0)
                {
                    try
                    {
                        var snapshotState = order.ToSnapshotState();
                        var snapshot = AggregateSnapshot.Create(
                            order.Id.Value.ToString(),
                            "Order",
                            order.Version,
                            JsonSerializer.Serialize(snapshotState));

                        await snapshotRepository.SaveAsync(snapshot, cancellationToken);

                        logger.LogInformation(
                            "Took snapshot for Order {OrderId} at version {Version}. " +
                            "Will replay from full event history on next load.",
                            orderId, order.Version);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "Failed to take snapshot for Order {OrderId} at version {Version}. " +
                            "Will replay from full event history on next load.",
                            orderId, order.Version);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed for Order {OrderId}", orderId);
                // rollback is automatic on Dispose, but fuck it's cleaner
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
    
    public async Task CreateOrderAsync(
        Order order,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);


            try
            {
                
                await eventStore.SaveEventsAsync(
                    order.Id.Value.ToString(),
                    "Order",
                    order.UncommitedEvents,
                    expectedVersion: -1,
                    cancellationToken);

                foreach (var domainEvent in order.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        domainEvent.EventId,
                        domainEvent.GetType().Name,
                        JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                        domainEvent.OccurredOn,
                        order.Id.Value.ToString(),
                        cancellationToken);
                }

                await idempotencyRepository.SaveAsync(
                    idempotencyKey,
                    order.Id.Value,
                    DateTime.UtcNow,
                    cancellationToken);

                order.ClearUncommittedEvents();

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                
                logger.LogInformation(
                    "Created Order {OrderId} with {EventCount} events",
                    order.Id.Value,
                    order.Version + 1);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed for order {OrderId}", order.Id.Value);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
    
    /*
     * Essentially it's merging old snapshot with new events. example:
     * snapshot chunk is 50 events, we have 67 events => 50 snapshot events merge 17 non-snapshot event
     * snapshot(v0) = { Status: Pending } = full object
     * delta(v1) = OrderPaidEvent { Status: Paid } = fact/event
     * result = { Status: Paid } = snapshot + delta (Apply method)
     */
    public async Task<Order?> LoadOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var snapshot = await snapshotRepository.GetLatestAsync(
            orderId.ToString(), "Order", cancellationToken);

        if (snapshot == null) return await ReplayOrderFromEventStoreAsync(orderId, cancellationToken);
        
        var snapshotState = JsonSerializer.Deserialize<OrderSnapshotState>(snapshot.StateJson);

        if (snapshotState is not null)
        {
            var order = Order.FromSnapshot(snapshotState);

            var deltaEvents = await eventStore.GetEventsAfterVersionAsync(
                orderId.ToString(), "Order", snapshot.Version + 1, cancellationToken);

            order.LoadFromHistory(deltaEvents);

            logger.LogDebug(
                "Loaded Order {OrderId} with snapshot v{SnapshotVersion} + {DeltaCount} delta events",
                orderId, snapshot.Version, deltaEvents.Count());

            return order;
        }

        logger.LogWarning(
            "Failed to deserialize snapshot for Order {OrderId}. Falling back to full replay.",
            orderId);

        return await ReplayOrderFromEventStoreAsync(orderId, cancellationToken);
    }

    private async Task<Order?> ReplayOrderFromEventStoreAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var events = await eventStore.GetEventsAsync(
            orderId.ToString(),
            "Order",
            cancellationToken);

        if (!events.Any())
            return null;

        return Order.FromHistory(events);
    }
}


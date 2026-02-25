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

    public async Task UpdateOrderAsync(
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
                
                await action(order);

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
                
                // @todo: check if order already exists. or we dont care about it?

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
    
    public async Task<Order?> LoadOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var snapshot = await snapshotRepository.GetLatestAsync(
            orderId.ToString(), "Order", cancellationToken);

        if (snapshot != null)
        {
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
        }

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


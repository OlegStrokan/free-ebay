using System.Data;
using System.Text.Json;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class OrderPersistenceService(
    IEventStoreRepository eventStore,
    IOutboxRepository outboxRepository,
    IIdempotencyRepository idempotencyRepository,
    AppDbContext dbContext,
    ILogger<OrderPersistenceService> logger) : IOrderPersistenceService
{
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
    
      private async Task<Order?> LoadOrderAsync(Guid orderId, CancellationToken cancellationToken)
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


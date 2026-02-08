using System.Data;
using System.Text.Json;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Services;


public class OrderPersistenceService(
    IOutboxRepository outboxRepository,
    IOrderRepository orderRepository,
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

                // @think: is this good way to load order?
                var order = await orderRepository.GetByIdAsync(OrderId.From(orderId), cancellationToken);

                if (order is null) throw new OrderNotFoundException(orderId);

                await action(order);

                foreach (var domainEvent in order.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        Guid.NewGuid(),
                        domainEvent.GetType().Name,
                        JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                        DateTime.UtcNow,
                        cancellationToken);
                }

                order.MarkEventsAsCommited();

                await dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed for Order {OrderId}", orderId);
                // @think: DELETE? rollback is automatic on Dispose, but fuck it's cleaner
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
    
    public async Task<Order> CreateOrderAsync(
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
                await orderRepository.AddAsync(order, cancellationToken);

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

                order.MarkEventsAsCommited();

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed for order {OrderId}", order.Id.Value);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
        
        return order;
    }
}


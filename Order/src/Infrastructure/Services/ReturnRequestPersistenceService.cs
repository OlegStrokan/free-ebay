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

public class ReturnRequestPersistenceService(
    IEventStoreRepository eventStore,
    IOutboxRepository outboxRepository,
    IIdempotencyRepository idempotencyRepository,
    AppDbContext dbContext,
    ILogger<ReturnRequestPersistenceService> logger)
    : IReturnRequestPersistenceService
{
    public async Task UpdateReturnRequestAsync(
        Guid orderId,
        Func<ReturnRequest, Task> action, 
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            try
            {
                var returnRequest = await LoadThisNastyBitch(orderId, cancellationToken);
                if (returnRequest is null)
                    throw new ReturnRequestNotFoundException(orderId);

                await action(returnRequest);
                
                await eventStore.SaveEventsAsync(
                    returnRequest.Id.Value.ToString(),
                    "ReturnRequest",
                    returnRequest.UncommitedEvents,
                    returnRequest.Version,
                    cancellationToken);
                    

                foreach (var domainEvent in returnRequest.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        domainEvent.EventId,
                        domainEvent.GetType().Name,
                        JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                        domainEvent.OccurredOn,
                        cancellationToken);
                }
                
                returnRequest.ClearUncommittedEvents();

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Transaction failed for ReturnRequest on Order {OrderId}", orderId);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<Guid> CreateReturnRequestAsync(
        ReturnRequest returnRequest,
        string? idempotencyKey = null,
        Guid? resultIdForIdempotency = null,
        CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            try
            {
                await eventStore.SaveEventsAsync(
                    returnRequest.Id.Value.ToString(),
                    "ReturnRequest",
                    returnRequest.UncommitedEvents,
                    expectedVersion: -1,
                    cancellationToken);
                
                foreach (var domainEvent in returnRequest.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        domainEvent.EventId,
                        domainEvent.GetType().Name,
                        JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                        domainEvent.OccurredOn,
                        cancellationToken);
                }

                if (!string.IsNullOrEmpty(idempotencyKey) && resultIdForIdempotency.HasValue)
                {
                    await idempotencyRepository.SaveAsync(
                        idempotencyKey,
                        resultIdForIdempotency.Value,
                        DateTime.UtcNow,
                        cancellationToken);
                }

                returnRequest.ClearUncommittedEvents();

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                
                logger.LogInformation(
                    "Created ReturnRequest {ReturnRequestId} with {EventCount} events",
                    returnRequest.Id.Value,
                    returnRequest.Version + 1);
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed for ReturnRequest");
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        return resultIdForIdempotency ?? returnRequest.OrderId.Value;
    }
    
    /*  dont judge me, i was high
        @todo: for better performance maybe maintain an index in read model type shit
        DONT USE THIS PEACE OF GARBAGE!
        DONT!
    */
    private async Task<ReturnRequest?> LoadThisNastyBitch(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        // this is not optimal, but i love ego lifting
        // for now we query DomainEvents directly to find ReturnRequest events
        var returnRequestEvents = await dbContext.DomainEvents
            .Where(e => e.AggregateType == "ReturnRequest")
            .OrderBy(e => e.OccuredOn)
            .ToListAsync(cancellationToken);
        
        // deserizl and check which returnRequest belong to this order
        foreach (var aggregateId in returnRequestEvents.Select(e => e.AggregateId).Distinct())
        {
            var events = await eventStore.GetEventsAsync(
                aggregateId,
                "ReturnRequest",
                cancellationToken);

            if (!events.Any())
                continue;

            var returnRequest = ReturnRequest.FromHistory(events);

            if (returnRequest.OrderId.Value == orderId)
                return returnRequest;
        }

        return null;
        
    }
    
    // this is preferred method to load returnRequests
    private async Task<ReturnRequest?> LoadReturnRequestAsync(
        Guid returnRequestId,
        CancellationToken cancellationToken)
    {
        var events = await eventStore.GetEventsAsync(
            returnRequestId.ToString(),
            "ReturnRequest",
            cancellationToken);

        if (!events.Any())
            return null;

        return ReturnRequest.FromHistory(events);
        
    }
    
    
}
using System.Data;
using System.Text.Json;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Entities.RequestReturn;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/* same as OrderPersistenceService: no dependency on the eventually-consistent read model
 * OrderId => ReturnRequestId mapping is written synchronously in the same transaction as
 * creation events, so LoadByOrderIdAsync is always strongly consistent
 */
public class ReturnRequestPersistenceService(
    IEventStoreRepository eventStore,
    IOutboxRepository outboxRepository,
    IIdempotencyRepository idempotencyRepository,
    IReturnRequestLookupRepository lookupRepository,
    AppDbContext dbContext,
    ILogger<ReturnRequestPersistenceService> logger)
    : IReturnRequestPersistenceService
{
    public async Task UpdateReturnRequestAsync(
        Guid orderId,
        Func<RequestReturn, Task> action,
        CancellationToken cancellationToken)
    {
        const int maxRetries = 3;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await TryUpdateReturnRequestAsync(orderId, action, cancellationToken);
                return;
            }
            catch (ConcurrencyConflictException) when (attempt < maxRetries)
            {
                logger.LogWarning(
                    "Concurrency conflict on attempt {Attempt}/{MaxRetries} for ReturnRequest on Order {OrderId}. Reloading and retrying...",
                    attempt, maxRetries, orderId);
            }
        }

        throw new ConcurrencyConflictException(AggregateTypes.ReturnRequest, orderId.ToString(), maxRetries);
    }

    private async Task TryUpdateReturnRequestAsync(
        Guid orderId,
        Func<RequestReturn, Task> action,
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            try
            {
                var returnRequest = await LoadByOrderIdAsync(orderId, cancellationToken);
                if (returnRequest is null)
                    throw new ReturnRequestNotFoundException(orderId);

                // Capture version before action(). action() calls RaiseEvent() which increments Version => inconsistency
                var expectedVersion = returnRequest.Version;

                await action(returnRequest);

                await eventStore.SaveEventsAsync(
                    returnRequest.Id.Value.ToString(),
                    AggregateTypes.ReturnRequest,
                    returnRequest.UncommitedEvents,
                    expectedVersion,
                    cancellationToken);

                foreach (var domainEvent in returnRequest.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        domainEvent.EventId,
                        domainEvent.GetType().Name,
                        JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                        domainEvent.OccurredOn,
                        returnRequest.Id.Value.ToString(),
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
        RequestReturn requestReturn,
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
                    requestReturn.Id.Value.ToString(),
                    AggregateTypes.ReturnRequest,
                    requestReturn.UncommitedEvents,
                    expectedVersion: -1,
                    cancellationToken);
                
                foreach (var domainEvent in requestReturn.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        domainEvent.EventId,
                        domainEvent.GetType().Name,
                        JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                        domainEvent.OccurredOn,
                        requestReturn.Id.Value.ToString(),
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

                // this is done for strict consistency which we need in requestReturn
                   await lookupRepository.AddAsync(
                    requestReturn.OrderId.Value,
                    requestReturn.Id.Value,
                    cancellationToken);

                requestReturn.ClearUncommittedEvents();

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                
                logger.LogInformation(
                    "Created ReturnRequest {ReturnRequestId} for Order {OrderId} with {EventCount} events",
                    requestReturn.Id.Value,
                    requestReturn.OrderId.Value,
                    requestReturn.Version + 1);
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed for ReturnRequest");
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        return resultIdForIdempotency ?? requestReturn.OrderId.Value;
    }

    private async Task<RequestReturn?> LoadReturnRequestAsync(
        Guid returnRequestId,
        CancellationToken cancellationToken)
    {
        var events = await eventStore.GetEventsAsync(
            returnRequestId.ToString(),
            AggregateTypes.ReturnRequest,
            cancellationToken);

        if (!events.Any())
            return null;

        return RequestReturn.FromHistory(events);
        
    }

    // why we call lookup repo, and then eventStore? 
    // because caller dont have returnRequestId, only orderId
    public async Task<RequestReturn?> LoadByOrderIdAsync(
        Guid orderId, CancellationToken cancellationToken)
    {
        // Lookup was written in the same transaction as creation — no eventual consistency gap.
        var returnRequestId = await lookupRepository.GetReturnRequestIdAsync(orderId, cancellationToken);

        if (returnRequestId is null)
            return null;

        return await LoadReturnRequestAsync(returnRequestId.Value, cancellationToken);
    }
     
    
}
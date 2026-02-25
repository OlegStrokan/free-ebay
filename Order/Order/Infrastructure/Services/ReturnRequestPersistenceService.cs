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
                var returnRequest = await LoadByOrderIdAsync(orderId, cancellationToken);
                if (returnRequest is null)
                    throw new ReturnRequestNotFoundException(orderId);

                // Capture version before action(). action() calls RaiseEvent() which increments Version => inconsisnency
                var expectedVersion = returnRequest.Version;

                await action(returnRequest);
                
                await eventStore.SaveEventsAsync(
                    returnRequest.Id.Value.ToString(),
                    "ReturnRequest",
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

    public async Task<ReturnRequest?> LoadByOrderIdAsync(
        Guid orderId, CancellationToken cancellationToken)
    {
        var readModel = await dbContext.ReturnRequestReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);

        if (readModel == null)
            return null;

        return await LoadReturnRequestAsync(readModel.Id, cancellationToken);
    }
     
    
}
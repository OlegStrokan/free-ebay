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
    IReturnRequestRepository returnRequestRepository,
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
                var returnRequest = await returnRequestRepository.GetByOrderIdAsync(
                    OrderId.From(orderId),
                    cancellationToken);

                if (returnRequest is null)
                    throw new ReturnRequestNotFoundException(orderId);

                await action(returnRequest);

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
                await returnRequestRepository.AddAsync(returnRequest, cancellationToken);

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
}
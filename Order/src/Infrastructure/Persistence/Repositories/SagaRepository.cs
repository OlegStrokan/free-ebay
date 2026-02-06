using System.Data;
using Application.Sagas;
using Application.Sagas.Persistence;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class SagaRepository(AppDbContext dbContext) : ISagaRepository
{
    public async Task<SagaState?> GetByIdAsync(Guid sagaId, CancellationToken cancellationToken)
    {
        return await dbContext.SagaStates
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.Id == sagaId, cancellationToken);
    }

    public async Task<SagaState?> GetByCorrelationIdAsync(Guid correlationId, string sagaType, CancellationToken cancellationToken)
    {
        return await dbContext.SagaStates
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x =>
                    x.CorrelationId == correlationId &&
                    x.SagaType == sagaType,
                cancellationToken);
    }

    public async Task SaveAsync(SagaState sagaState, CancellationToken cancellationToken)
    {
        var existing = await dbContext.SagaStates.AnyAsync(x => x.Id == sagaState.Id, cancellationToken);

        if (!existing)
        {
            await dbContext.SagaStates.AddAsync(sagaState, cancellationToken);
        }
        else
        {
            dbContext.SagaStates.Update(sagaState);
        }
    }

    public Task<SagaState?> GetByIdAsync<TContext>(Guid sagaId, CancellationToken cancellationToken) where TContext : SagaContext
    {
        throw new NotImplementedException();
    }

    public Task<SagaState?> GetByCorrelationIdAsync<TContext>(Guid correlationId, string sagaType, CancellationToken cancellationToken) where TContext : SagaContext
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync<TContext>(SagaState sagaState, CancellationToken cancellationToken) where TContext : SagaContext
    {
        throw new NotImplementedException();
    }

    public async Task SaveStepAsync(SagaStepLog stepLog, CancellationToken cancellationToken)
    {
        await dbContext.SagaStepLogs.AddAsync(stepLog, cancellationToken);
    }

    public Task<List<SagaState>> GetStuckSagasAsync<TContext>(TimeSpan timeout, CancellationToken cancellationToken) where TContext : SagaContext
    {
        throw new NotImplementedException();
    }

    public async Task<List<SagaState>> GetStuckSagasAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeout);

        return await dbContext.SagaStates
            .Where(x => x.Status == SagaStatus.Running && x.UpdatedAt < cutoffTime)
            .Include(x => x.Steps)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveCompensationStateAsync(
        SagaState sagaState, 
        SagaStepLog stepLog,
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            try
            {
                dbContext.SagaStepLogs.Update(stepLog);
                dbContext.SagaStates.Update(sagaState);

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
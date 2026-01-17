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
        
        //@think: debatable
       // await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveStepAsync(SagaStepLog stepLog, CancellationToken cancellationToken)
    {
        await dbContext.SagaStepLogs.AddAsync(stepLog, cancellationToken);
    }

    public async Task<List<SagaState>> GetStuckSagasAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeout);

        return await dbContext.SagaStates
            .Where(x => x.Status == SagaStatus.Running && x.UpdatedAt < cutoffTime)
            .Include(x => x.Steps)
            .ToListAsync(cancellationToken);
    }
}
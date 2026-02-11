using Application.Interfaces;
using Infrastructure.Persistence.DbContext;

namespace Infrastructure.Persistence;

// @todo: probably should be deleted
public sealed class UnitOfWork(
    AppDbContext context,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{

    
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Beginning new database transaction");
        
        var efTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
        
        logger.LogInformation(
            "Database transaction started with ID {TransactionId}",
            efTransaction.TransactionId);
        
        return new EfCoreTransactionWrapper(efTransaction, logger);

    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var changesCount = await context.SaveChangesAsync(cancellationToken);

        logger.LogDebug(
            "Saved {ChangeCount} changes to database",
            changesCount);

        return changesCount;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
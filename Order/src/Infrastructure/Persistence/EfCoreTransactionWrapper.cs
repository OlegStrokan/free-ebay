using Application.Interfaces;

namespace Infrastructure.Persistence;

// @todo: probably should be deleted
// adapter between application IDbContextTransaction and infrastructure IDbContextTransaction
internal sealed class EfCoreTransactionWrapper(
    Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction efTransaction,
    ILogger logger) : IDbContextTransaction
{

    private bool _disposed;

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(EfCoreTransactionWrapper));
        }

        try
        {
            logger.LogDebug("Committing transaction {TransactionId}",
                efTransaction.TransactionId);

            await efTransaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Transaction {TransactionId} commited successfully",
                efTransaction.TransactionId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to commit transaction {TransactionId}",
                efTransaction.TransactionId);
            throw;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EfCoreTransactionWrapper));

        try
        {
            logger.LogWarning(
                "Rolling back transaction {TransactionId}",
                efTransaction.TransactionId);

            await efTransaction.RollbackAsync(cancellationToken);

            logger.LogInformation(
                "Transaction {TransactionId} rolled back",
                efTransaction.TransactionId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to rollback transaction {TransactionId}",
                efTransaction.TransactionId);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        logger.LogDebug(
            "Disposing transaction {TransactionId}",
            efTransaction.TransactionId);

        await efTransaction.DisposeAsync();
        _disposed = true;

    }
}
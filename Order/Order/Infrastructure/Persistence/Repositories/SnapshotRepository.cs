using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class SnapshotRepository(
    AppDbContext dbContext,
    ILogger<SnapshotRepository> logger) : ISnapshotRepository
{
    public async Task<AggregateSnapshot?> GetLatestAsync(
        string aggregateId,
        string aggregateType,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.AggregateSnapshots
            .AsNoTracking()
            .Where(s => s.AggregateId == aggregateId && s.AggregateType == aggregateType)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(AggregateSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        // we never update a snapshot - each version is immutable
        // if the same version already exists (retry case), skip silently

        var exists = await dbContext.AggregateSnapshots
            .AnyAsync(s =>
                    s.AggregateId == snapshot.AggregateId &&
                    s.AggregateType == snapshot.AggregateType &&
                    s.Version == snapshot.Version,
                cancellationToken);

        if (exists)
        {
            logger.LogDebug(
                "Snapshot for {AggregateType} {AggregateId} version {Version} already exists. Skipping",
                snapshot.AggregateType, snapshot.AggregateId, snapshot.Version);
            return;
        }

        dbContext.AggregateSnapshots.Add(snapshot);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation(
            "Saved snapshot for {AggregateType} {AggregateId} at version {Version}",
            snapshot.AggregateType, snapshot.AggregateId, snapshot.Version);
    }
}
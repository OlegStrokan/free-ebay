using Domain.Entities;

namespace Domain.Interfaces;

public interface ISnapshotRepository
{
    Task<AggregateSnapshot?> GetLatestAsync(
        string aggregateId,
        string aggregateType,
        CancellationToken cancellationToken);
    
    Task SaveAsync(AggregateSnapshot snapshot, CancellationToken cancellationToken);
}
using Domain.Common;

namespace Domain.Interfaces;

public interface IEventStoreRepository
{
    Task SaveEventsAsync(
        string aggregateId,
        string aggregateType,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<IDomainEvent>>GetEventsAsync(
        string aggregateId,
        string aggregateType,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string aggregateId,
        string aggregateType,
        CancellationToken cancellationToken = default);
}
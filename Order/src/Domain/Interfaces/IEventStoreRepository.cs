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

// @todo: deadcode - should be deleted or used
    Task<bool> ExistsAsync(
        string aggregateId,
        string aggregateType,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<IDomainEvent>> GetEventsAfterVersionAsync(
        string aggregateId,
        string aggregateType,
        int afterVersion,
        CancellationToken cancellationToken = default);
}
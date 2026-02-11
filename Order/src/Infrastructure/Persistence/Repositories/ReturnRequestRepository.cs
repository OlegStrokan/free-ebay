using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Infrastructure.Persistence.Repositories;

public class ReturnRequestRepository(IEventStoreRepository eventStore) : IReturnRequestRepository
{
    public async Task<ReturnRequest?> GetByIdAsync(ReturnRequestId id, CancellationToken ct = default)
    {
        var events = await eventStore.GetEventsAsync(
            id.Value.ToString(),
            "ReturnRequest",
            ct);

        if (!events.Any())
            return null;

        return ReturnRequest.FromHistory(events);
    }

    public async Task AddAsync(ReturnRequest returnRequest, CancellationToken ct = default)
    {
        // For new aggregates, expected version is -1
        await eventStore.SaveEventsAsync(
            returnRequest.Id.Value.ToString(),
            "ReturnRequest",
            returnRequest.UncommitedEvents,
            expectedVersion: -1,
            ct);
    }

    public async Task<ReturnRequest?> GetByOrderIdAsync(
        OrderId orderId,
        CancellationToken ct = default)
    {
        // Note: This requires a query index on OrderId in your event store
        // For now, this is a simplified version
        // In production, you'd need to maintain a read model/index for this query
        
        // todo: implement proper query via read model
        throw new NotImplementedException(
            "GetByOrderIdAsync requires a read model index. " +
            "Consider creating a ReturnRequestReadModel for queries.");
    }

}
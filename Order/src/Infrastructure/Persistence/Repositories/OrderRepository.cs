using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;

namespace Infrastructure.Persistence.Repositories;

public class OrderRepository(IEventStoreRepository eventStore) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default)
    {
        var events = await eventStore.GetEventsAsync(
            id.Value.ToString(),
            "Order",
            ct);

        if (!events.Any())
            return null;

        return Order.FromHistory(events);
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        // for new aggregates, expected version is -1
        await eventStore.SaveEventsAsync(
            order.Id.Value.ToString(),
            "Order",
            order.UncommitedEvents,
            expectedVersion: -1,
            ct);
    }

    public async Task<bool> ExistsAsync(OrderId orderId, CancellationToken ct = default)
    {
        return await eventStore.ExistsAsync(
            orderId.Value.ToString(),
            "Order",
            ct);
    }
}
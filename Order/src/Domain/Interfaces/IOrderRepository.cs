using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Interfaces;


    public interface IOrderRepository
    {
        // Loads the Aggregate by replaying events via Order.FromEvents()
        Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default);
        // Persists the UncommittedEvents to the Event Store
        Task AddAsync(Order order, CancellationToken ct = default);
        // check if exists without loading full aggregate
        Task<bool> ExistsAsync(OrderId orderId, CancellationToken ct = default);
    }

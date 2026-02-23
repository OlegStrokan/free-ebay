using Domain.Entities;
using Domain.Entities.Order;

namespace Application.Interfaces;

public interface IOrderPersistenceService
{

    Task CreateOrderAsync(
        Order order,
        string idempotencyKey,
        CancellationToken cancellationToken);
    
    Task UpdateOrderAsync(
        Guid orderId,
        Func<Order, Task> action,
        CancellationToken ct);

    Task<Order?> LoadOrderAsync(Guid orderId, CancellationToken ct);
}
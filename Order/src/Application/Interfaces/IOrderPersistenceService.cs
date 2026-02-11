using Domain.Entities;

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

}
using Domain.Entities;

namespace Application.Interfaces;

public interface ISagaOrderPersistenceService
{
    Task UpdateOrderAsync(
        Guid orderId,
        Func<Order, Task> action,
        CancellationToken ct);

}
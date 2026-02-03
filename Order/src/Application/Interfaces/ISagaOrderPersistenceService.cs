using Domain.Entities;

namespace Application.Interfaces;

public interface ISagaOrderPersistenceService
{
    Task<Order?> LoadOrderAsync(Guid orderId, CancellationToken ct);

    Task<TResult> ExecuteAsync<TResult>(
        Guid orderId,
        Func<Order, TResult> action,
        CancellationToken ct);

    Task<TResult> ExecuteAsync<TResult>(
        Guid orderId,
        Func<Order, Task<TResult>> action,
        CancellationToken ct);
}
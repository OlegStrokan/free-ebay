using Domain.Entities.B2BOrder;

namespace Application.Interfaces;

public interface IB2BOrderPersistenceService
{
    Task StartB2BOrderAsync(B2BOrder order, string idempotencyKey, CancellationToken ct);
    Task UpdateB2BOrderAsync(Guid b2bOrderId, Func<B2BOrder, Task> action, CancellationToken ct);
    Task<B2BOrder?> LoadB2BOrderAsync(Guid b2bOrderId, CancellationToken ct);
}

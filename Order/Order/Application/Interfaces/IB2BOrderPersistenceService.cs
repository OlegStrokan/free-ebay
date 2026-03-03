using Domain.Entities;
using Domain.Entities.B2BOrder;
using Domain.Entities.Order;

namespace Application.Interfaces;

public interface IB2BOrderPersistenceService
{
    Task StartB2BOrderAsync(B2BOrder order, string idempotencyKey, CancellationToken ct);
    Task UpdateB2BOrderAsync(Guid b2bOrderId, Func<B2BOrder, Task> action, CancellationToken ct);
    Task FinalizeB2BOrderAsync(Guid b2bOrderId, Order orderToCreate, string idempotencyKey, CancellationToken ct);
    Task<B2BOrder?> LoadB2BOrderAsync(Guid b2bOrderId, CancellationToken ct);
}

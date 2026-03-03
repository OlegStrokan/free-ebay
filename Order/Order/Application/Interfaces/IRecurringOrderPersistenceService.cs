using Domain.Entities.Subscription;

namespace Application.Interfaces;

public interface IRecurringOrderPersistenceService
{
    Task CreateAsync(RecurringOrder order, string idempotencyKey, CancellationToken ct);
    Task UpdateAsync(Guid recurringOrderId, Func<RecurringOrder, Task> action, CancellationToken ct);
    Task<Guid> ExecuteAsync(Guid recurringOrderId, CancellationToken ct);
    Task<RecurringOrder?> LoadAsync(Guid recurringOrderId, CancellationToken ct);
}
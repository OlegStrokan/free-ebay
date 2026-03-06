using Domain.Entities;
using Domain.Entities.RequestReturn;

namespace Application.Interfaces;

public interface IReturnRequestPersistenceService
{
    Task UpdateReturnRequestAsync(
        Guid orderId,
        Func<RequestReturn, Task> action,
        CancellationToken cancellationToken);

    Task<Guid> CreateReturnRequestAsync(
        RequestReturn requestReturn,
        string? idempotencyKey = null,
        Guid? orderIdForIdempotency = null,
        CancellationToken cancellationToken = default);

    Task<RequestReturn?> LoadByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken);
}
using Domain.Entities;

namespace Application.Interfaces;

public interface IReturnRequestPersistenceService
{
    Task UpdateReturnRequestAsync(
        Guid orderId,
        Func<ReturnRequest, Task> action,
        CancellationToken cancellationToken);


    Task<Guid> CreateReturnRequestAsync(
        ReturnRequest returnRequest,
        string? idempotencyKey = null,
        Guid? orderIdForIdempotency = null,
        CancellationToken cancellationToken = default);

}
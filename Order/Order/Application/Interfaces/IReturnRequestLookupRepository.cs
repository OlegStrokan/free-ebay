namespace Application.Interfaces;

public interface IReturnRequestLookupRepository
{
    Task AddAsync(Guid orderId, Guid returnRequestId, CancellationToken cancellationToken);

    Task<Guid?> GetReturnRequestIdAsync(Guid orderId, CancellationToken cancellationToken);
}

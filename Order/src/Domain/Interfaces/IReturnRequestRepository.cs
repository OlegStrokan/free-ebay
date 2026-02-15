using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Interfaces;

//@todo: should be deleted and overwritten in all saga steps by persistence service
public interface IReturnRequestRepository
{
    Task<ReturnRequest?> GetByIdAsync(ReturnRequestId id, CancellationToken ct = default);
    Task AddAsync(ReturnRequest returnRequest, CancellationToken ct = default);
    Task<ReturnRequest?> GetByOrderIdAsync(OrderId orderId, CancellationToken ct = default);
}
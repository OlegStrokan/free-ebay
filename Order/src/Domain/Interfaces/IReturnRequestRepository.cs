using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Interfaces;

public interface IReturnRequestRepository
{
    Task<ReturnRequest?> GetByIdAsync(ReturnRequestId id, CancellationToken ct = default);
    Task AddAsync(ReturnRequest returnRequest, CancellationToken ct = default);
    Task<ReturnRequest?> GetByOrderIdAsync(OrderId orderId, CancellationToken ct = default);
}
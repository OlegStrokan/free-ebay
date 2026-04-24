using Application.Interfaces;

namespace Application.Interfaces;

public interface IRecurringOrderReadRepository
{
    Task<RecurringOrderDetail?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<RecurringOrderSummary>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<List<RecurringOrderSummary>> GetDueAsync(DateTime asOf, int limit, CancellationToken ct = default);
    Task<List<RecurringOrderSummary>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default);
}

public sealed record RecurringOrderDetail(
    Guid Id,
    Guid CustomerId,
    string PaymentMethod,
    string Frequency,
    string Status,
    DateTime NextRunAt,
    DateTime? LastRunAt,
    int TotalExecutions,
    int? MaxExecutions,
    AddressResponse DeliveryAddress,
    List<RecurringOrderItemResponse> Items,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int Version);

public sealed record RecurringOrderSummary(
    Guid Id,
    Guid CustomerId,
    string Frequency,
    string Status,
    DateTime NextRunAt,
    int TotalExecutions,
    DateTime CreatedAt);

public sealed record RecurringOrderItemResponse(
    Guid   ProductId,
    int    Quantity,
    decimal Price,
    string Currency);

namespace Application.Interfaces;

public interface IOrderReadRepository
{
    Task<OrderResponse?> GetByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<OrderResponse?> GetGyTrackingIdAsync(Guid trackingId, CancellationToken ct = default);
    Task<List<OrderSummaryResponse>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<List<OrderSummaryResponse>> GetOrderAsync(int pageNumber, int pageSize, CancellationToken ct = default);
}

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    Guid TrackingId,
    string Status,
    decimal TotalAmount,
    string Currency,
    AddressResponse DeliveryAddress,
    List<OrderItemResponse> Items,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int Version
    );


public sealed record OrderSummaryResponse(
    Guid Id,
    Guid TrackingId,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt
);

public sealed record OrderItemResponse(
    Guid ProductId,
    int Quality,
    decimal Price,
    string Currency
    );
    
    
public sealed record AddressResponse(
    string Street,
    string City, 
    string State,
    string Country,
    string PostalCode
    );
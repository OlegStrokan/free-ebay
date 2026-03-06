namespace Application.Interfaces;

public interface IOrderReadRepository
{
    Task<OrderResponse?> GetByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<OrderResponse?> GetByTrackingIdAsync(string trackingId, CancellationToken ct = default);
    Task<List<OrderSummaryResponse>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<List<OrderSummaryResponse>> GetOrderAsync(int pageNumber, int pageSize, CancellationToken ct = default);
}

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string? TrackingId,
    string? PaymentId,
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
    string? TrackingId,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt
);

public sealed record OrderItemResponse(
    Guid ProductId,
    int Quantity,
    decimal Price,
    string Currency
    );
    
    
public sealed record AddressResponse(
    string Street,
    string City, 
    string Country,
    string PostalCode
    );
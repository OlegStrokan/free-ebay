using Gateway.Api.Contracts.Common;

namespace Gateway.Api.Contracts.Orders;

public sealed record CreateOrderRequest(
    string CustomerId,
    IReadOnlyList<OrderItemDto> Items,
    AddressDto DeliveryAddress,
    string PaymentMethod,
    string IdempotencyKey);

public sealed record CreateOrderResponse(bool Success, string OrderId, string? ErrorMessage);

public sealed record RequestReturnRequest(
    string Reason,
    IReadOnlyList<OrderItemDto> ItemsToReturn,
    string IdempotencyKey);

public sealed record RequestReturnResponse(bool Success, string ReturnRequestId, string? ErrorMessage);

public sealed record OrderDetailsResponse(
    string Id,
    string CustomerId,
    string TrackingId,
    string PaymentId,
    string Status,
    decimal TotalAmount,
    string Currency,
    AddressDto DeliveryAddress,
    IReadOnlyList<OrderItemDetailResponse> Items,
    string CreatedAt,
    string UpdatedAt,
    int Version);

public sealed record OrderItemDetailResponse(string ProductId, int Quantity, decimal Price, string Currency);

public sealed record OrderSummaryResponse(
    string Id,
    string TrackingId,
    string Status,
    decimal TotalAmount,
    string Currency,
    string CreatedAt);

using Gateway.Api.Contracts.Common;

namespace Gateway.Api.Contracts.RecurringOrders;

public sealed record CreateRecurringOrderRequest(
    string CustomerId,
    string PaymentMethod,
    string Frequency,
    IReadOnlyList<OrderItemDto> Items,
    AddressDto DeliveryAddress,
    string? FirstRunAt,
    int MaxExecutions,
    string IdempotencyKey);

public sealed record RecurringOrderActionResponse(bool Success, string RecurringOrderId, string? ErrorMessage);

public sealed record CancelRecurringOrderRequest(string Reason);

public sealed record RecurringOrderDetailsResponse(
    string Id,
    string CustomerId,
    string PaymentMethod,
    string Frequency,
    string Status,
    string NextRunAt,
    string LastRunAt,
    int TotalExecutions,
    int MaxExecutions,
    AddressDto DeliveryAddress,
    IReadOnlyList<RecurringOrderItemResponse> Items,
    string CreatedAt,
    string UpdatedAt,
    int Version);

public sealed record RecurringOrderItemResponse(string ProductId, int Quantity, decimal Price, string Currency);

public sealed record RecurringOrderSummaryResponse(
    string Id,
    string Frequency,
    string Status,
    string NextRunAt,
    int TotalExecutions,
    string CreatedAt);

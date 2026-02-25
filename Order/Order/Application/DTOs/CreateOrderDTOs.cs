namespace Application.DTOs;

// @todo: deadcode - should be deleted or used
public sealed record CreateOrderDto(
    Guid CustomerId,
    List<OrderItemDto> Items,
    AddressDto DeliveryAddress,
    string PaymentMethod,
    string? IdempotencyKey
);


public sealed record OrderItemDto(
    Guid ProductId,
    int Quantity,
    decimal Price,
    string Currency
);

public sealed record AddressDto(
    string Street,
    string City,
    string Country,
    string PostalCode
);
    
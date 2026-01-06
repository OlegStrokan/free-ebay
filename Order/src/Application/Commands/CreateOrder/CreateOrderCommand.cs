using Application.Common;
using Application.DTOs;

namespace Application.Commands.CreateOrder;

public record CreateOrderCommand(
    Guid CustomerId,
    List<OrderItemDto> Items,
    AddressDto DeliveryAddress,
    string PaymentMethod,
    string IdempotencyKey) : ICommand<Result<Guid>>;
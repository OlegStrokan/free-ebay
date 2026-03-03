using Application.Common;
using Application.DTOs;
using MediatR;

namespace Application.Commands.RecurringOrder.CreateRecurringOrder;

public record CreateRecurringOrderCommand(
    Guid   CustomerId,
    string PaymentMethod,
    string Frequency,
    List<RecurringItemDto> Items,
    AddressDto DeliveryAddress,
    DateTime?  FirstRunAt    = null,
    int?       MaxExecutions = null,
    string     IdempotencyKey = "") : IRequest<Result<Guid>>;

public record RecurringItemDto(Guid ProductId, int Quantity, decimal Price, string Currency);

using Application.Common;
using Application.DTOs;
using MediatR;

namespace Application.Commands.RequestReturn;

public record RequestReturnCommand(
    Guid OrderId,
    string Reason,
    List<OrderItemDto> ItemsToReturn,
    string IdempotencyKey
    ) : IRequest<Result<Guid>>;
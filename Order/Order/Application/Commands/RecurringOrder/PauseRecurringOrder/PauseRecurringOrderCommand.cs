using Application.Common;
using MediatR;

namespace Application.Commands.RecurringOrder.PauseRecurringOrder;

public record PauseRecurringOrderCommand(Guid RecurringOrderId) : IRequest<Result<Guid>>;

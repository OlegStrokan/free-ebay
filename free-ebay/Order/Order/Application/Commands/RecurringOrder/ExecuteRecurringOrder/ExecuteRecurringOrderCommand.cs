using Application.Common;
using MediatR;

namespace Application.Commands.RecurringOrder.ExecuteRecurringOrder;

public record ExecuteRecurringOrderCommand(Guid RecurringOrderId) : IRequest<Result<Guid>>;

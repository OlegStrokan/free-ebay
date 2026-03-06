using Application.Common;
using MediatR;

namespace Application.Commands.RecurringOrder.CancelRecurringOrder;

public record CancelRecurringOrderCommand(Guid RecurringOrderId, string Reason) : IRequest<Result<Guid>>;

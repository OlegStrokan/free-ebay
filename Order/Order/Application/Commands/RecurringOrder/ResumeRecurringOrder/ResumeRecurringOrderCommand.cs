using Application.Common;
using MediatR;

namespace Application.Commands.RecurringOrder.ResumeRecurringOrder;

public record ResumeRecurringOrderCommand(Guid RecurringOrderId) : IRequest<Result<Guid>>;

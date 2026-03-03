using Application.Common;
using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.RecurringOrder.ExecuteRecurringOrder;

public sealed class ExecuteRecurringOrderCommandHandler(
    IRecurringOrderPersistenceService persistenceService,
    ILogger<ExecuteRecurringOrderCommandHandler> logger)
    : IRequestHandler<ExecuteRecurringOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        ExecuteRecurringOrderCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            //  load the read-side cheaply before the heavy write path
            var order = await persistenceService.LoadAsync(request.RecurringOrderId, cancellationToken);

            if (order is null)
            {
                logger.LogWarning("RecurringOrder {Id} not found — skipping execution", request.RecurringOrderId);
                return Result<Guid>.Failure("RecurringOrder not found");
            }

            if (!order.IsDue)
            {
                // already executed by a concurrent scheduler instance or paused/cancelled
                logger.LogInformation(
                    "RecurringOrder {Id} is not due or not active (Status={Status}, NextRunAt={NextRunAt}). Skipping.",
                    request.RecurringOrderId, order.Status, order.NextRunAt);
                return Result<Guid>.Success(Guid.Empty);
            }

            var createdOrderId = await persistenceService.ExecuteAsync(
                request.RecurringOrderId, cancellationToken);

            logger.LogInformation(
                "RecurringOrder {Id} executed — child Order {OrderId} created. Execution #{N}",
                request.RecurringOrderId, createdOrderId, order.TotalExecutions + 1);

            return Result<Guid>.Success(createdOrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing RecurringOrder {Id}", request.RecurringOrderId);
            return Result<Guid>.Failure(ex.Message);
        }
    }
}

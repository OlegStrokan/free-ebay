using Application.Common;
using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.RecurringOrder.PauseRecurringOrder;

public sealed class PauseRecurringOrderCommandHandler(
    IRecurringOrderPersistenceService persistenceService,
    ILogger<PauseRecurringOrderCommandHandler> logger)
    : IRequestHandler<PauseRecurringOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(PauseRecurringOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await persistenceService.UpdateAsync(
                request.RecurringOrderId,
                order => { order.Pause(); return Task.CompletedTask; },
                cancellationToken);

            logger.LogInformation("RecurringOrder {Id} paused", request.RecurringOrderId);
            return Result<Guid>.Success(request.RecurringOrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error pausing RecurringOrder {Id}", request.RecurringOrderId);
            return Result<Guid>.Failure(ex.Message);
        }
    }
}

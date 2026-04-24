using Application.Common;
using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.RecurringOrder.CancelRecurringOrder;

public sealed class CancelRecurringOrderCommandHandler(
    IRecurringOrderPersistenceService persistenceService,
    ILogger<CancelRecurringOrderCommandHandler> logger)
    : IRequestHandler<CancelRecurringOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CancelRecurringOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await persistenceService.UpdateAsync(
                request.RecurringOrderId,
                order => { order.Cancel(request.Reason); return Task.CompletedTask; },
                cancellationToken);

            logger.LogInformation("RecurringOrder {Id} cancelled", request.RecurringOrderId);
            return Result<Guid>.Success(request.RecurringOrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling RecurringOrder {Id}", request.RecurringOrderId);
            return Result<Guid>.Failure(ex.Message);
        }
    }
}

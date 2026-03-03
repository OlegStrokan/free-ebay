using Application.Common;
using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.RecurringOrder.ResumeRecurringOrder;

public sealed class ResumeRecurringOrderCommandHandler(
    IRecurringOrderPersistenceService persistenceService,
    ILogger<ResumeRecurringOrderCommandHandler> logger)
    : IRequestHandler<ResumeRecurringOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ResumeRecurringOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await persistenceService.UpdateAsync(
                request.RecurringOrderId,
                order => { order.Resume(); return Task.CompletedTask; },
                cancellationToken);

            logger.LogInformation("RecurringOrder {Id} resumed", request.RecurringOrderId);
            return Result<Guid>.Success(request.RecurringOrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resuming RecurringOrder {Id}", request.RecurringOrderId);
            return Result<Guid>.Failure(ex.Message);
        }
    }
}

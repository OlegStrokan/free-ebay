using Application.Common;
using Application.Interfaces;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.CancelB2BOrder;

public class CancelB2BOrderCommandHandler(
    IB2BOrderPersistenceService persistenceService,
    ILogger<CancelB2BOrderCommandHandler> logger)
    : IRequestHandler<CancelB2BOrderCommand, Result>
{
    public async Task<Result> Handle(CancelB2BOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await persistenceService.UpdateB2BOrderAsync(
                request.B2BOrderId,
                order =>
                {
                    order.Cancel(request.Reasons);
                    return Task.CompletedTask;
                },
                cancellationToken);

            logger.LogInformation("Cancelled B2BOrder {B2BOrderId}", request.B2BOrderId);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Domain violation cancelling B2BOrder {B2BOrderId}", request.B2BOrderId);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cancel B2BOrder {B2BOrderId}", request.B2BOrderId);
            return Result.Failure(ex.Message);
        }
    }
}

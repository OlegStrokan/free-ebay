using Application.Interfaces;
using Application.Sagas.Steps;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.OrderSaga.Steps;

// Compensate cancels the order, ensuring cancellation happens regardless of which downstream step fails
// @think: is this good architectural appraoch?
public sealed class CancelOrderOnFailureStep(
    IOrderPersistenceService orderPersistenceService,
    ILogger<CancelOrderOnFailureStep> logger)
    : ISagaStep<OrderSagaData, OrderSagaContext>
{
    public string StepName => "CancelOrderOnFailure";
    public int Order => 0;

    public Task<StepResult> ExecuteAsync(
        OrderSagaData data,
        OrderSagaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(StepResult.SuccessResult());

    public async Task CompensateAsync(
        OrderSagaData data,
        OrderSagaContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Cancelling order {OrderId} due to saga compensation", data.CorrelationId);

            await orderPersistenceService.UpdateOrderAsync(
                data.CorrelationId,
                order =>
                {
                    order.Cancel(["Saga compensation - order creation failed"]);
                    return Task.CompletedTask;
                },
                cancellationToken);

            logger.LogInformation("Order {OrderId} cancelled successfully", data.CorrelationId);
        }
        catch (OrderNotFoundException)
        {
            logger.LogWarning(
                "Compensate skipped: Order {OrderId} not found. It may have never been created.",
                data.CorrelationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cancel order {OrderId} during compensation", data.CorrelationId);
        }
    }
}

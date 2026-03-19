using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.OrderSaga.Steps;

public sealed class AwaitPaymentConfirmationStep(
    ILogger<AwaitPaymentConfirmationStep> logger)
    : ISagaStep<OrderSagaData, OrderSagaContext>
{
    public string StepName => "AwaitPaymentConfirmation";
    public int Order => 3;

    public Task<StepResult> ExecuteAsync(
        OrderSagaData data,
        OrderSagaContext context,
        CancellationToken cancellationToken)
    {
        /* Backward compatibility for in-flight saga contexts that predate PaymentStatus.
        -----------
        if (context.PaymentStatus == OrderSagaPaymentStatus.NotStarted && !string.IsNullOrEmpty(context.PaymentId))
        {
            context.PaymentStatus = OrderSagaPaymentStatus.Succeeded;
        }
        --------
        * we dont need it, because we ca redeploy our service and no in flight snapshots exists,
        * but we we have rolling deploy without purguing saga state we definitely need in specific use-case:
        * user place order, saga created and goes throught steps, then saga wait for payment be approved
        * in external service so saga paused. then we deploy ned code where context should have PaymentStatus,
        * but saga which is paused didn't have it, so after deploy, after kafka receive payment successfull 
        * state, we need to check if context have paymentId, because in old code it's meant what payment was successfull,
        * but in new code we also should have context.paymentStatus which succesfful, but old saga dont have, so we force to have it
        */

        return Task.FromResult(context.PaymentStatus switch
        {
            OrderSagaPaymentStatus.Succeeded => HandleSucceeded(data, context),
            OrderSagaPaymentStatus.Failed => HandleFailed(data, context),
            OrderSagaPaymentStatus.Pending or OrderSagaPaymentStatus.RequiresAction => HandleWaiting(data, context),
            _ => StepResult.Failure("Payment state is unknown. Cannot continue order saga."),
        });
    }

    private StepResult HandleSucceeded(OrderSagaData data, OrderSagaContext context)
    {
        if (string.IsNullOrWhiteSpace(context.PaymentId))
        {
            return StepResult.Failure("Payment was marked as succeeded but PaymentId is missing.");
        }

        logger.LogInformation(
            "Payment confirmation received for order {OrderId}. Continuing saga.",
            data.CorrelationId);

        return StepResult.SuccessResult(new Dictionary<string, object>
        {
            ["PaymentId"] = context.PaymentId,
            ["Status"] = context.PaymentStatus.ToString(),
        });
    }

    private StepResult HandleFailed(OrderSagaData data, OrderSagaContext context)
    {
        var reason = context.PaymentFailureMessage ?? "Payment failed callback received";

        logger.LogWarning(
            "Payment failed for order {OrderId}. Triggering compensation. Reason: {Reason}",
            data.CorrelationId,
            reason);

        return StepResult.Failure($"Payment failed: {reason}");
    }

    private StepResult HandleWaiting(OrderSagaData data, OrderSagaContext context)
    {
        logger.LogInformation(
            "Order {OrderId} is still waiting for payment finalization. Status: {Status}",
            data.CorrelationId,
            context.PaymentStatus);

        return StepResult.SuccessResult(
            data: new Dictionary<string, object>
            {
                ["PaymentId"] = context.PaymentId ?? string.Empty,
                ["Status"] = context.PaymentStatus.ToString(),
            },
            metadata: new Dictionary<string, object>
            {
                ["SagaState"] = "WaitingForEvent"
            });
    }

    public Task CompensateAsync(
        OrderSagaData data,
        OrderSagaContext context,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}

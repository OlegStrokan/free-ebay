using Application.Gateways;
using Application.Gateways.Exceptions;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.OrderSaga.Steps;


public sealed class ProcessPaymentStep(
    IPaymentGateway paymentGateway,
    ILogger<ProcessPaymentStep> logger)
    : ISagaStep<OrderSagaData, OrderSagaContext>
{
    public string StepName => "ProcessPayment";
    public int Order => 2;

    public async Task<StepResult> ExecuteAsync(
        OrderSagaData data,
        OrderSagaContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Processing payment for order {OrderId}, amount {Amount} {Currency}",
                data.CorrelationId,
                data.TotalAmount,
                data.Currency
            );

            var paymentId = await paymentGateway.ProcessPaymentAsync(
                orderId: data.CorrelationId,
                customerId: data.CustomerId,
                amount: data.TotalAmount,
                currency: data.Currency,
                paymentMethod: data.PaymentMethod,
                cancellationToken);

            context.PaymentId = paymentId;
            
            logger.LogInformation(
                "Successfully processed payment {PaymentId} for order {OrderId}",
                paymentId,
                data.CorrelationId
                );

            return StepResult.SuccessResult(new Dictionary<string, object>
            {
                ["PaymentId"] = paymentId,
                ["Amount"] = data.TotalAmount,
                ["Currency"] = data.Currency
            });
        }
        catch (PaymentDeclinedException ex)
        {
            logger.LogWarning(
                ex,
                "Payment declined for order {OrderId}",
                data.CorrelationId
            );

            return StepResult.Failure($"Payment declined: {ex.Message}");
        }
        catch (InsufficientFundsException ex)
        {
            logger.LogWarning(
                ex,
                "Insufficient funds for order {OrderId}",
                data.CorrelationId);

            return StepResult.Failure($"Insufficient funds: {ex.Message}");
        }

        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Payment processing failed for order {OrderId}",
                data.CorrelationId
                );

            return StepResult.Failure($"Payment failed: {ex.Message}");
        }
    }

    public async Task CompensateAsync(
        OrderSagaData data,
        OrderSagaContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.PaymentId))
        {
            logger.LogInformation(
                "No payment to refund for order {OrderId}",
                data.CorrelationId
                );
            return;
        }

        try
        {
            logger.LogInformation(
                "Refunding payment {PaymentId} for order {OrderId}",
                context.PaymentId,
                data.CorrelationId
            );

            await paymentGateway.RefundAsync(
                paymentId: context.PaymentId,
                amount: data.TotalAmount,
                reason: "Order cancelled - saga compensation",
                cancellationToken);

            logger.LogInformation(
                "Successfully refunded payment {PaymentId}",
                context.PaymentId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to refund payment {PaymentId}. Manual refund required!",
                context.PaymentId);
            
            // log for manual intervention
            // could also send alert to operations team
        }

        
    }
}
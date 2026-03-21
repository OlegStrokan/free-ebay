using Application.Common.Enums;
using Application.Gateways;
using Application.Gateways.Exceptions;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.OrderSaga.Steps;


public sealed class ProcessPaymentStep(
    IPaymentGateway paymentGateway,
    IIncidentReporter incidentReporter,
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
            // Backward compatibility for snapshots saved before PaymentStatus existed.
            if (context.PaymentStatus == OrderSagaPaymentStatus.NotStarted && !string.IsNullOrEmpty(context.PaymentId))
            {
                context.PaymentStatus = OrderSagaPaymentStatus.Succeeded;
            }

            if (context.PaymentStatus == OrderSagaPaymentStatus.Succeeded && !string.IsNullOrEmpty(context.PaymentId))
            {
                logger.LogInformation(
                    "Payment already processed for order {OrderId} with PaymentId {PaymentId}. Skipping.",
                    data.CorrelationId,
                    context.PaymentId);

                return StepResult.SuccessResult(new Dictionary<string, object>
                {
                    ["PaymentId"] = context.PaymentId,
                });
            }

            if (context.PaymentStatus == OrderSagaPaymentStatus.Failed)
            {
                var error = context.PaymentFailureMessage ?? "Payment was marked as failed by callback";
                logger.LogWarning(
                    "Payment already failed for order {OrderId}. Error: {Error}",
                    data.CorrelationId,
                    error);
                return StepResult.Failure($"Payment failed: {error}");
            }

            if (context.PaymentStatus is
                OrderSagaPaymentStatus.Pending or 
                OrderSagaPaymentStatus.RequiresAction or
                OrderSagaPaymentStatus.Uncertain)
            {
                logger.LogInformation(
                    "Payment is still awaiting provider confirmation for order {OrderId}. " +
                    "Current status: {Status}",
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
            
            logger.LogInformation(
                "Processing payment for order {OrderId}, amount {Amount} {Currency}",
                data.CorrelationId,
                data.TotalAmount,
                data.Currency
            );

            var paymentResult = await paymentGateway.ProcessPaymentAsync(
                orderId: data.CorrelationId,
                customerId: data.CustomerId,
                amount: data.TotalAmount,
                currency: data.Currency,
                paymentMethod: data.PaymentMethod,
                cancellationToken);

            context.PaymentId = paymentResult.PaymentId;
            context.ProviderPaymentIntentId = paymentResult.ProviderPaymentIntentId;
            context.PaymentClientSecret = paymentResult.ClientSecret;
            context.PaymentFailureCode = paymentResult.ErrorCode;
            context.PaymentFailureMessage = paymentResult.ErrorMessage;

            switch (paymentResult.Status)
            {
                case PaymentProcessingStatus.Succeeded:
                    context.PaymentStatus = OrderSagaPaymentStatus.Succeeded;
                    logger.LogInformation(
                        "Successfully processed payment {PaymentId} for order {OrderId}",
                        context.PaymentId,
                        data.CorrelationId);

                    return StepResult.SuccessResult(new Dictionary<string, object>
                    {
                        ["PaymentId"] = context.PaymentId ?? string.Empty,
                        ["Amount"] = data.TotalAmount,
                        ["Currency"] = data.Currency,
                        ["Status"] = context.PaymentStatus.ToString(),
                    });

                case PaymentProcessingStatus.Pending:
                    context.PaymentStatus = OrderSagaPaymentStatus.Pending;
                    logger.LogInformation(
                        "Payment {PaymentId} for order {OrderId} is pending provider confirmation",
                        context.PaymentId,
                        data.CorrelationId);

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

                case PaymentProcessingStatus.RequiresAction:
                    context.PaymentStatus = OrderSagaPaymentStatus.RequiresAction;
                    logger.LogInformation(
                        "Payment {PaymentId} for order {OrderId} requires customer action before completion",
                        context.PaymentId,
                        data.CorrelationId);

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

                case PaymentProcessingStatus.Failed:
                    context.PaymentStatus = OrderSagaPaymentStatus.Failed;
                    var failedMessage = paymentResult.ErrorMessage ?? "Provider returned failed status";
                    logger.LogWarning(
                        "Payment failed for order {OrderId}. Error: {Error}",
                        data.CorrelationId,
                        failedMessage);

                    return StepResult.Failure($"Payment failed: {failedMessage}");

                default:
                    context.PaymentStatus = OrderSagaPaymentStatus.Failed;
                    return StepResult.Failure("Payment returned unknown processing status");
            }
        }
        catch (PaymentDeclinedException ex)
        {
            context.PaymentStatus = OrderSagaPaymentStatus.Failed;
            context.PaymentFailureCode = "PAYMENT_DECLINED";
            context.PaymentFailureMessage = ex.Message;

            logger.LogWarning(
                ex,
                "Payment declined for order {OrderId}",
                data.CorrelationId
            );

            return StepResult.Failure($"Payment declined: {ex.Message}");
        }
        catch (InsufficientFundsException ex)
        {
            context.PaymentStatus = OrderSagaPaymentStatus.Failed;
            context.PaymentFailureCode = "INSUFFICIENT_FUNDS";
            context.PaymentFailureMessage = ex.Message;

            logger.LogWarning(
                ex,
                "Insufficient funds for order {OrderId}",
                data.CorrelationId);

            return StepResult.Failure($"Insufficient funds: {ex.Message}");
        }
        catch (GatewayUnavailableException ex) when (ex.Reason == GatewayUnavailableReason.Timeout)
        {
           context.PaymentStatus = OrderSagaPaymentStatus.Uncertain;
            context.PaymentFailureCode = "PAYMENT_RESULT_UNCERTAIN";
            context.PaymentFailureMessage = ex.Message;

            logger.LogWarning(
                ex,
                "Payment call timed out for order {OrderId}. Marking as Uncertain and waiting for webhook/reconciliation",
                data.CorrelationId);

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
        catch (GatewayUnavailableException ex)
        {
            context.PaymentStatus = OrderSagaPaymentStatus.Failed;
            context.PaymentFailureCode = "PAYMENT_GATEWAY_UNAVAILABLE";
            context.PaymentFailureMessage = ex.Message;

            logger.LogError(
                ex,
                "Payment service unavailable for order {OrderId}. Cannot determine payment result",
                data.CorrelationId);

            return StepResult.Failure($"Payment gateway unavailable: {ex.Message}");
        }

        catch (Exception ex)
        {
            context.PaymentStatus = OrderSagaPaymentStatus.Failed;
            context.PaymentFailureCode = "PAYMENT_ERROR";
            context.PaymentFailureMessage = ex.Message;

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
        // Backward compatibility for snapshots saved before PaymentStatus existed.
        if (context.PaymentStatus == OrderSagaPaymentStatus.NotStarted && !string.IsNullOrEmpty(context.PaymentId))
        {
            context.PaymentStatus = OrderSagaPaymentStatus.Succeeded;
        }

        if (string.IsNullOrEmpty(context.PaymentId))
        {
            logger.LogInformation(
                "No payment to refund for order {OrderId}",
                data.CorrelationId
                );
            return;
        }

        // Skip refund if payment is Uncertain - it might succeed later via webhook/reconciliation
        // Skip if Failed - we can't refund what wasn't charged
        // @think: is this correct?
        if (context.PaymentStatus is OrderSagaPaymentStatus.Uncertain or OrderSagaPaymentStatus.Failed)
        {
            logger.LogInformation(
                "Skipping refund for order {OrderId}. Payment status is {Status}.",
                data.CorrelationId,
                context.PaymentStatus);
            return;
        }

        if (context.PaymentStatus != OrderSagaPaymentStatus.Succeeded)
        {
            logger.LogInformation(
                "Skipping refund for order {OrderId}. Payment status is {Status}.",
                data.CorrelationId,
                context.PaymentStatus);
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
                currency: data.Currency,
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

            await incidentReporter.SendAlertAsync(
                new IncidentAlert(
                    AlertType: "PaymentRefundCompensationFailed",
                    OrderId: data.CorrelationId,
                    RefundId: null,
                    Message: $"Failed to refund payment {context.PaymentId} during saga compensation",
                    Severity: AlertSeverity.Critical),
                cancellationToken);
        }

        
    }
}
using Application.Interfaces;
using Application.Sagas.Steps;
using Domain.Exceptions;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.OrderSaga.Steps;

public class UpdateOrderStatusStep(
    ISagaOrderPersistenceService orderPersistenceService,
    ILogger<UpdateOrderStatusStep> logger
    )
    : ISagaStep<OrderSagaData, OrderSagaContext>
{
    public string StepName => "UpdateOrderStatus";
    public int Order => 3;

    public async Task<StepResult> ExecuteAsync(
        OrderSagaData data,
        OrderSagaContext context, 
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Updating order {OrderId} status to Paid",
                data.CorrelationId);

            if (context.OrderStatusUpdated)
            {
                logger.LogInformation(
                    "Order {OrderId} status already updated, skipping",
                    data.CorrelationId);
                return StepResult.SuccessResult();
            }

            if (string.IsNullOrEmpty(context.PaymentId))
                return StepResult.Failure("Payment ID not found in saga context");

            await orderPersistenceService.UpdateOrderAsync(
                data.CorrelationId,
                order =>
                {
                    var paymentId = PaymentId.From(context.PaymentId);
                    order.Pay(paymentId);
                    return Task.CompletedTask;
                },
                cancellationToken);

            context.OrderStatusUpdated = true;

            logger.LogInformation(
                "Successfully updated order {OrderId} to Paid status",
                data.CorrelationId);

            return StepResult.SuccessResult(new Dictionary<string, object>
            {
                ["OrderId"] = data.CorrelationId,
                ["Status"] = "Paid"
            });
        }
        catch (OrderNotFoundException ex)
        { 
            //return StepResult.Failure($"Order {data.CorrelationId} not found");
            // Specific handling: Maybe this order shouldn't exist? 
            // We return a failure that tells the Saga to stop and compensate.
            return StepResult.Failure($"Critical Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to update order {OrderId} status",
                data.CorrelationId);

            return StepResult.Failure($"Failed to update order status: {ex.Message}");
        }
    }

    public async Task CompensateAsync(
        OrderSagaData data,
        OrderSagaContext context, 
        CancellationToken cancellationToken)
    {

        try
        {
            logger.LogInformation(
                "Cancelling order {OrderId}",
                data.CorrelationId);

            await orderPersistenceService.UpdateOrderAsync(
                data.CorrelationId,
                order =>
                {
                    var failureMessages = new List<string> { "Saga compensation - order creation failed" };
                    order.Cancel(failureMessages);
                    return Task.CompletedTask;
                },
                cancellationToken);

            logger.LogInformation("Successfully cancelled order {OrderId}",
                data.CorrelationId);

        }
        catch (OrderNotFoundException ex)
        {
            logger.LogWarning("Compensate skipped: Order {OrderId} not found. It may have never been created.", data.CorrelationId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to cancel order {OrderId} during compensation",
                data.CorrelationId);
        }
    }
}
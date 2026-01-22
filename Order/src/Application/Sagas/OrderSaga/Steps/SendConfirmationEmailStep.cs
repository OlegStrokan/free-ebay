using Application.Gateways;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.OrderSaga.Steps;

public sealed class SendConfirmationEmailStep(
        IEmailGateway _emailGateway,
        ILogger<SendConfirmationEmailStep> _logger
    ) : ISagaStep<OrderSagaData, OrderSagaContext>
{

    public string StepName => "SendConfirmationEmail";
    public int Order => 5;


    public async Task<StepResult> ExecuteAsync(
        OrderSagaData data,
        OrderSagaContext context,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogInformation(
                "Sending confirmation email for order {OrderId} to customer {CustomerId}",
                data.CorrelationId,
                data.CustomerId);


            await _emailGateway.SendOrderConfirmationAsync(
                customerId: data.CustomerId,
                orderId: data.CorrelationId,
                orderTotal: data.TotalAmount,
                currency: data.Currency,
                items: data.Items,
                deliveryDelivery: data.DeliveryAddress,
                estimatedDelivery: DateTime.UtcNow.AddDays(5),
                cancellationToken);

            return StepResult.SuccessResult(new Dictionary<string, object>
            {
                ["EmailSent"] = true,
                ["CustomerId"] = data.CustomerId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send confirmation email for order {OrderId}",
                data.CorrelationId);
            
            // email failure shouldn't fail the entire saga
            // log and continue - can retry later or notify customer through other channels

            return StepResult.SuccessResult(new Dictionary<string, object>
            {
                ["EmailSent"] = false,
                ["Warning"] = "Email failed but order is complete"
            });
        }
    }

    public async Task CompensateAsync(OrderSagaData data, OrderSagaContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "No compensation needed for confirmation email step (order {OrderId})",
            data.CorrelationId);

        await Task.CompletedTask;
    }
}
using Application.Gateways;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.ReturnSaga.Steps;

public sealed class AwaitReturnShipmentStep(
    IShippingGateway shippingGateway,
    ILogger<AwaitReturnShipmentStep> logger
    ) : ISagaStep<ReturnSagaData, ReturnSagaContext>
{
    public string StepName => "AwaitReturnShipment";
    public int Order => 2;

    public async Task<StepResult> ExecuteAsync(
        ReturnSagaData data,
        ReturnSagaContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Initiating return shipment for order {OrderId}",
                data.CorrelationId);
            
            // create return shipment label
            var returnShipmentId = await shippingGateway.CreateReturnShipmentAsync(
                orderId: data.CorrelationId,
                customerId: data.CustomerId,
                items: data.ReturnedItems,
                cancellationToken
            );

            context.ReturnShipmentId = returnShipmentId;
            
            logger.LogInformation(
                "Return shipment created {ShipmentId} for order {OrderId}. Awaiting delivery...",
                returnShipmentId,
                data.CorrelationId);
            
            await shippingGateway.RegisterWebhookAsync(
                shipmentId: returnShipmentId,
                // todo: add real link 
                callbackUrl: "should be updated ",
                events: ["return.delivered"],
                cancellationToken);
            
            
            return StepResult.SuccessResult(new Dictionary<string, object>
            {
                ["ReturnShipmentId"] = returnShipmentId,
                ["Status"] = "WaitingForDelivery",
                ["SagaState"] = "WaitingForEvent",
                ["Message"] = "Return label created. Waiting for customer to ship package."
            });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to process return shipment for order {OrderId}",
                data.CorrelationId);

            return StepResult.Failure($"Return shipment failed: {ex.Message}");
        }
    }

    public async Task CompensateAsync(
        ReturnSagaData data,
        ReturnSagaContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.ReturnShipmentId))
        {
            logger.LogInformation(
                "No return shipment to cancel for order {OrderId}",
                data.CorrelationId);
            return;
        }

        try
        {
            logger.LogInformation(
                "Cancelling return shipment {ShipmentId} for order {OrderId}",
                context.ReturnShipmentId,
                data.CorrelationId);

            await shippingGateway.CancelReturnShipmentAsync(
                returnShipmentId: context.ReturnShipmentId,
                reason: "Return saga compensation - return request cancelled",
                cancellationToken);

            logger.LogInformation(
                "Successfully cancelled return shipment {ShipmentId}",
                context.ReturnShipmentId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to cancel return shipment {ShipmentId}. Manual intervention required",
                context.ReturnShipmentId);
        }

    }
}
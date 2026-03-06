using Application.Common.Enums;
using Application.Gateways;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.ReturnSaga.Steps;

public sealed class AwaitReturnShipmentStep(
    IShippingGateway shippingGateway,
    IIncidentReporter incidentReporter,
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

            string returnShipmentId;
            
            if (string.IsNullOrEmpty(context.ReturnShipmentId))
            {
            
                var shipmentResult = await shippingGateway.CreateReturnShipmentAsync(
                    orderId: data.CorrelationId,
                    customerId: data.CustomerId,
                    items: data.ReturnedItems,
                    cancellationToken
                );

                returnShipmentId = shipmentResult.ReturnShipmentId;
                context.ReturnShipmentId = returnShipmentId;
                logger.LogInformation("Created return shipment {Id}", returnShipmentId);

            }
            else
            {
                returnShipmentId = context.ReturnShipmentId;
                logger.LogInformation("Shipment {Id} already exists. Skipping creation.", returnShipmentId);
            }
            
            
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
            
            
            return StepResult.SuccessResult(
                data: new Dictionary<string, object>
                {
                    ["ReturnShipmentId"] = returnShipmentId,
                    ["Status"] = "WaitingForDelivery",
                    ["Message"] = "Return label created. Waiting for customer to ship package."
                },
                metadata: new Dictionary<string, object>
                {
                    ["SagaState"] = "WaitingForEvent"
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

            await incidentReporter.CreateInterventionTicketAsync(
                new InterventionTicket(
                    OrderId: data.CorrelationId,
                    RefundId: null,
                    Issue: $"Failed to cancel return shipment {context.ReturnShipmentId} during compensation",
                    SuggestedAction: "Manually cancel return shipment with shipping provider"),
                cancellationToken);
        }

    }
}
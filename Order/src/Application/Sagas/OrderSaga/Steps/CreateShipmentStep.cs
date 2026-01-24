using Application.Gateways;
using Application.Gateways.Exceptions;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.OrderSaga.Steps;

public sealed class CreateShipmentStep(
    IShippingGateway _shippingGateway,
    ILogger<CreateShipmentStep> _logger
    ) : ISagaStep<OrderSagaData, OrderSagaContext>
{
    public string StepName => "CreateShipment";
    public int Order => 4;
    
    public async Task<StepResult> ExecuteAsync(OrderSagaData data, OrderSagaContext context, CancellationToken cancellationToken)
    {
        await using 
        try
        {
            _logger.LogInformation(
                "Creating shipment for order {OrderId}",
                data.CorrelationId);

            var shipmentId = await _shippingGateway.CreateShipmentAsync(
                orderId: data.CorrelationId,
                deliveryAddress: data.DeliveryAddress,
                items: data.Items,
                cancellationToken);
            
            
            context.ShipmentId = shipmentId;
            
            // todo get tracking number
            // order.AssignTracking(trackingNumber);

            _logger.LogInformation(
                "Successfully created shipment {ShipmentId} for order {OrderId}",
                shipmentId,
                data.CorrelationId);

            return StepResult.SuccessResult(new Dictionary<string, object>
            {
                ["ShipmentId"] = shipmentId,
                ["DeliveryAddress"] = $"{data.DeliveryAddress.Street}, {data.DeliveryAddress.City}"
            });
        }
        catch (InvalidAddressException ex)
        {
            _logger.LogInformation(
                ex,
                "Invalid delivery address for order {OrderId}",
                data.CorrelationId);

            return StepResult.Failure($"Invalid delivery address: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid delivery address for order {OrderId}",
                data.CorrelationId);

            return StepResult.Failure($"Invalid delivery address: {ex.Message}");
        }
    }

    public async Task CompensateAsync(OrderSagaData data, OrderSagaContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.ShipmentId))
        {
            _logger.LogInformation("No shipment to cancel for order {OrderId}", data.CorrelationId);
            return;
        }

        try
        {
            _logger.LogInformation("Cancelling shipment {ShipmentId} for order {OrderId}",
                context.ShipmentId,
                data.CorrelationId);

            await _shippingGateway.CancelShipmentAsync(
                shipmentId: context.ShipmentId,
                reason: "Order cancelled - saga compensation",
                cancellationToken);
            
            _logger.LogInformation(
                "Successfully cancelled shipment {ShipmentId}",
                context.ShipmentId);
        }
        
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to cancel shipment {ShipmentId}. Manual cancellation may be required",
                context.ShipmentId);
        }
    }

}
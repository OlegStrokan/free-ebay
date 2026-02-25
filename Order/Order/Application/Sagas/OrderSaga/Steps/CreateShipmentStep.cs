using System.Text.Json;
using Application.Gateways;
using Application.Gateways.Exceptions;
using Application.Interfaces;
using Application.Sagas.Steps;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.OrderSaga.Steps;

public sealed class CreateShipmentStep(
    IShippingGateway shippingGateway,
    IOrderPersistenceService orderPersistenceService,
    ILogger<CreateShipmentStep> logger
    ) : ISagaStep<OrderSagaData, OrderSagaContext>
{
    public string StepName => "CreateShipment";
    public int Order => 4;
    
    public async Task<StepResult> ExecuteAsync(OrderSagaData data, OrderSagaContext context, CancellationToken cancellationToken)
    {
        try
        {

            if (!string.IsNullOrEmpty(context.ShipmentId))
            {
                logger.LogInformation(
                    "Shipment already reserved with {ShipmentId}. Skipping.",
                    context.ShipmentId);

                return StepResult.SuccessResult(new Dictionary<string, object>
                {
                    ["ShipmentId"] = context.ShipmentId,
                    ["Idempotent"] = true
                });
            }

            logger.LogInformation(
                "Creating shipment for order {OrderId}",
                data.CorrelationId);
            
            var (shipmentId, trackingNumber) = await shippingGateway.CreateShipmentAsync(
                orderId: data.CorrelationId,
                deliveryAddress: data.DeliveryAddress,
                items: data.Items,
                cancellationToken);

            context.ShipmentId = shipmentId;
            context.TrackingNumber = trackingNumber;

            logger.LogInformation(
                "Tracking number retrieved: {TrackingNumber}",
                trackingNumber);

            await orderPersistenceService.UpdateOrderAsync(
                data.CorrelationId,
                order =>
                {
                    var trackingId = TrackingId.From(trackingNumber);
                    order.AssignTracking(trackingId);
                    return Task.CompletedTask;
                },
                cancellationToken);

            logger.LogInformation(
                "Successfully created shipment {ShipmentId} with tracking {TrackingNumber} for order {OrderId}",
                shipmentId,
                trackingNumber,
                data.CorrelationId);

            return StepResult.SuccessResult(new Dictionary<string, object>
            {
                ["ShipmentId"] = shipmentId,
                ["DeliveryAddress"] = $"{data.DeliveryAddress.Street}, {data.DeliveryAddress.City}"
            });
        }
        catch (OrderNotFoundException ex)
        {
            logger.LogInformation(
                ex,
                "Order with ID {OrderId} not found ",
                data.CorrelationId);
                return StepResult.Failure($"Order {data.CorrelationId} not found");
        }
        catch (InvalidAddressException ex)
        {
            logger.LogInformation(
                ex,
                "Invalid delivery address for order {OrderId}",
                data.CorrelationId);

            return StepResult.Failure($"Invalid delivery address: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(
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
            logger.LogInformation("No shipment to cancel for order {OrderId}", data.CorrelationId);
            return;
        }
        
        try
        {
            logger.LogInformation("Compensating shipment {ShipmentId} for order {OrderId}",
                context.ShipmentId,
                data.CorrelationId);

            try
            {
                
                await shippingGateway.CancelShipmentAsync(
                    shipmentId: context.ShipmentId,
                    cancellationToken);

                logger.LogInformation(
                    "Successfully cancelled shipment {ShipmentId}",
                    context.ShipmentId);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to cancel shipment {ShipmentId}. Manual cancellation may be required",
                    context.ShipmentId);
                // don't throw - continue to revert order tracking
            }

            await orderPersistenceService.UpdateOrderAsync(
                data.CorrelationId,
                order =>
                {
                    order.RevertTrackingAssignment();
                    return Task.CompletedTask;
                },
                cancellationToken);
            
            logger.LogInformation(
                "Successfully compensated shipment step for order {OrderId}",
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
                "Failed to compensate shipment {ShipmentId}. Manual intervention required",
                context.ShipmentId);
        }
    }
    }


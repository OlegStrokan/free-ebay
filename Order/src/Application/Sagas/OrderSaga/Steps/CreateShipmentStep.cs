using System.Text.Json;
using Application.Gateways;
using Application.Gateways.Exceptions;
using Application.Interfaces;
using Application.Sagas.Steps;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.OrderSaga.Steps;

public sealed class CreateShipmentStep(
    IShippingGateway _shippingGateway,
    IOrderRepository orderRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateShipmentStep> _logger
    ) : ISagaStep<OrderSagaData, OrderSagaContext>
{
    public string StepName => "CreateShipment";
    public int Order => 4;
    
    public async Task<StepResult> ExecuteAsync(OrderSagaData data, OrderSagaContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken); 
        try
        {
            _logger.LogInformation(
                "Creating shipment for order {OrderId}",
                data.CorrelationId);

            // da fuck? where is idempotency? @todo
            var shipmentId = await _shippingGateway.CreateShipmentAsync(
                orderId: data.CorrelationId,
                deliveryAddress: data.DeliveryAddress,
                items: data.Items,
                cancellationToken);
            
            
            context.ShipmentId = shipmentId;


            var trackingNumber = await _shippingGateway.GetTrackingNumberAsync(
                shipmentId, cancellationToken);
            
            
            // @think: should we? we just need trackingNumber for assigning tracking in order aggregate
            // context.TrackingNumber = trackingNumber

            _logger.LogInformation(
                "Tracking number retrieved: {TrackingNumber}",
                trackingNumber);

            var order = await orderRepository.GetByIdAsync(
                OrderId.From(data.CorrelationId),
                cancellationToken);

            if (order == null)
                return StepResult.Failure($"Order {data.CorrelationId} not found");

            var trackingId = TrackingId.From(trackingNumber);
            
            order.AssignTracking(trackingId);

            await orderRepository.AddAsync(order, cancellationToken);

            foreach (var domainEvent in order.UncommitedEvents)
            {
                await outboxRepository.AddAsync(
                    domainEvent.EventId,
                    domainEvent.GetType().Name,
                    JsonSerializer.Serialize(domainEvent),
                    domainEvent.OccurredOn,
                    cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            order.MarkEventsAsCommited();

            
            _logger.LogInformation(
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

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Compensating shipment {ShipmentId} for order {OrderId}",
                context.ShipmentId,
                data.CorrelationId);

            try
            {


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
                // don't throw - continue to revert order tracking
            }

            var order = await orderRepository.GetByIdAsync(
                OrderId.From(data.CorrelationId),
                cancellationToken);

            if (order != null)
            {
                _logger.LogInformation(
                    "Removing tracking {TrackingNumber} from {OrderId}",
                    order.TrackingId,
                    data.CorrelationId);

                order.RevertTrackingAssignment();

                await orderRepository.AddAsync(order, cancellationToken);

                foreach (var domainEvent in order.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        domainEvent.EventId,
                        domainEvent.GetType().Name,
                        JsonSerializer.Serialize(domainEvent),
                        domainEvent.OccurredOn,
                        cancellationToken);
                }

                _logger.LogInformation(
                    "Tracking removed from order {OrderId}",
                    data.CorrelationId);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            _logger.LogInformation(
                "Successfully compensated shipment step for order {OrderId}",
                data.CorrelationId);
        }
        catch (Exception ex)
        {
            
            _logger.LogError(
                ex,
                "Failed to compensate shipment {ShipmentId}. Manual intervention required",
                context.ShipmentId);
        }
    }
    }


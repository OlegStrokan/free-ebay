using Application.Gateways;
using Application.Gateways.Exceptions;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.OrderSaga.Steps;

public sealed class ReserveInventoryStep(
    IInventoryGateway inventoryGateway,
    ILogger<ReserveInventoryStep> logger)
    : ISagaStep<OrderSagaData, OrderSagaContext>
{
    public string StepName => "ReserveInventory";
    public int Order => 1;

    public async Task<StepResult> ExecuteAsync(OrderSagaData data,
        OrderSagaContext context, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Reserving inventory for order {OrderId} with {ItemCount} items",
                data.CorrelationId,
                data.Items.Count);

            // have you ever heard about idempotency type shit? @todo
            
            var reservationId = await inventoryGateway.ReserveAsync(
                orderId: data.CorrelationId,
                items: data.Items,
                cancellationToken);

            context.ReservationId = reservationId;

            logger.LogInformation(
                "Successfully reserved inventory {ReservationId} for order {OrderId}",
                reservationId,
                data.CorrelationId);

            return StepResult.SuccessResult(new Dictionary<string, object>
            {
                ["ReservationId"] = reservationId,
                ["ItemsReversed"] = data.Items.Count
            });
        }
        catch (InsufficientInventoryException ex)
        {
            logger.LogWarning(
                ex, "Insufficient inventory for order {OrderId}",
                data.CorrelationId);

            return StepResult.Failure($"Insufficient inventory: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to reserve inventory for order {OrderId}",
                data.CorrelationId);

            return StepResult.Failure($"Inventory reservation failed: {ex.Message}");
        }
    } 

    public async Task CompensateAsync(OrderSagaData data, 
        OrderSagaContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.ReservationId))
        {
            logger.LogInformation(
                "No reservation to release for order {OrderId}",
                data.CorrelationId);
            return;
        }

        try
        {
            logger.LogInformation("Releasing inventory reservation {ReservationId} for order {OrderId}",
                context.ReservationId,
                data.CorrelationId);

            await inventoryGateway.ReleaseReservationAsync(context.ReservationId, cancellationToken);
            
            logger.LogInformation("Successfully released reservation {ReservationId}", context.ReservationId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to release inventory reservation {ReservationId}. Manual intervention may be required",
                context.ReservationId);
            
            // don't throw - we want to continue compensating other steps
            // logs this for manual review/cleanup
        }
    }
}

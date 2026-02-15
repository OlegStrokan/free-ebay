using Application.DTOs;
using Application.Gateways;
using Application.Gateways.Exceptions;
using Grpc.Core;
using Protos.Inventory;

namespace Infrastructure.Gateways;

public sealed class InventoryGateway(
    InventoryService.InventoryServiceClient client,
    ILogger<InventoryGateway> logger) : IInventoryGateway
{
    public async Task<string> ReserveAsync(
        Guid orderId, 
        List<OrderItemDto> items, 
        CancellationToken cancellationToken)
    {
        var request = new ReserveInventoryRequest
        {
            OrderId = orderId.ToString()
        };
        
        request.Items.AddRange(items.Select(i => new InventoryItem
        {
            ProductId = i.ProductId.ToString(),
            Quality = i.Quantity
        }));

        try
        {
            var response = await client.ReserveInventoryAsync(request, cancellationToken: cancellationToken);

            if (!response.Success)
                throw new InsufficientExecutionStackException(
                    $"Inventory reservation failed for OrderId={orderId}. Reason={response.ErrorMessage}");

            logger.LogInformation(
                "Inventory reserved successfully. OrderId={OrderId}, ReservationId={ReservationId}",
                orderId,
                response.ReservationId);

            return response.ReservationId;
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.FailedPrecondition or StatusCode.ResourceExhausted)
        {
            throw new InsufficientInventoryException(
                $"Insufficient inventory for OrderId={orderId}. gRPC={ex.StatusCode}: {ex.Status.Detail}");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new InsufficientInventoryException(
                $"One or more products not found for OrderId={orderId}. Detail={ex.Status.Detail}");
        }
    }

    public async Task ReleaseReservationAsync(
        string reservationId,
        CancellationToken cancellationToken)
    {
        var request = new ReleaseInventoryRequest
        {
            ReservationId = reservationId
        };


        try
        {
            var response = await client.ReleaseInventoryAsync(request, cancellationToken: cancellationToken);

            if (!response.Success)
                throw new InvalidOperationException(
                    $"Inventory release failed for ReservationId={reservationId}. Message={response.ErrorMessage}");

            logger.LogInformation(
                "Inventory released successfully. ReservationId={ReservationId}", reservationId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            // idempotent release: already gone
            logger.LogWarning(
                "ReleaseReservation: reservation not found (treated as idempotent success). ReservationId={ReservationId}",
                reservationId);
        }
    }
}
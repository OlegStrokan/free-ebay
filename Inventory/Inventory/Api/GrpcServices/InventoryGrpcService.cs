using Application.Interfaces;
using Application.Models;
using Grpc.Core;
using Protos.Inventory;
using StatusCode = Grpc.Core.StatusCode;

namespace Api.GrpcServices;

// @think: this is code smell. too much voodoo for grpc layer.
// right now i dont give a fuck, but i am aware of that
public sealed class InventoryGrpcService(
    IInventoryService inventoryService,
    ILogger<InventoryGrpcService> logger) : InventoryService.InventoryServiceBase
{
    public override async Task<ReserveInventoryResponse> ReserveInventory(
        ReserveInventoryRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.OrderId, out var orderId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "order_id must be a valid GUID"));

        var items = new List<ReserveInventoryItemInput>(request.Items.Count);

        foreach (var item in request.Items)
        {
            if (!Guid.TryParse(item.ProductId, out var productId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "every product_id must be a valid GUID"));
            }

            items.Add(new ReserveInventoryItemInput(productId, item.Quantity));
        }

        ReserveInventoryResult result;

        try
        {
            result = await inventoryService.ReserveAsync(
                new ReserveInventoryCommand(orderId, items),
                context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while reserving inventory. OrderId={OrderId}", request.OrderId);
            throw new RpcException(new Status(StatusCode.Unavailable, "Inventory service unavailable"));
        }

        if (result.Success)
        {
            return new ReserveInventoryResponse
            {
                Success = true,
                ReservationId = result.ReservationId,
                ErrorMessage = result.IsIdempotentReplay ? "Idempotent replay." : string.Empty
            };
        }

        throw result.FailureReason switch
        {
            ReserveInventoryFailureReason.Validation =>
                new RpcException(new Status(StatusCode.InvalidArgument, result.ErrorMessage)),
            ReserveInventoryFailureReason.ProductNotFound =>
                new RpcException(new Status(StatusCode.NotFound, result.ErrorMessage)),
            ReserveInventoryFailureReason.InsufficientStock =>
                new RpcException(new Status(StatusCode.FailedPrecondition, result.ErrorMessage)),
            _ => new RpcException(new Status(
                StatusCode.Unavailable,
                string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "Inventory reservation failed."
                    : result.ErrorMessage))
        };
    }

    public override async Task<ConfirmReservationResponse> ConfirmReservation(
        ConfirmReservationRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.ReservationId, out var reservationId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "reservation_id must be a valid GUID"));

        ReleaseInventoryResult result;

        try
        {
            result = await inventoryService.ConfirmAsync(reservationId, context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error while confirming inventory reservation. ReservationId={ReservationId}",
                request.ReservationId);

            throw new RpcException(new Status(StatusCode.Unavailable, "Inventory service unavailable"));
        }

        if (!result.Success)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, result.ErrorMessage));
        }

        return new ConfirmReservationResponse
        {
            Success = true,
            ErrorMessage = result.ErrorMessage
        };
    }

    public override async Task<ReleaseInventoryResponse> ReleaseInventory(
        ReleaseInventoryRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.ReservationId, out var reservationId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "reservation_id must be a valid GUID"));

        ReleaseInventoryResult result;

        try
        {
            result = await inventoryService.ReleaseAsync(reservationId, context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error while releasing inventory. ReservationId={ReservationId}",
                request.ReservationId);

            throw new RpcException(new Status(StatusCode.Unavailable, "Inventory service unavailable"));
        }

        return new ReleaseInventoryResponse
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}

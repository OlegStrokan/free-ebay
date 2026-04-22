using Application.DTOs;
using Application.DTOs.ShipmentGateway;

namespace Infrastructure.Gateways.Carrier;

internal interface ICarrierAdapter
{
    Task<ShipmentResultDto> CreateShipmentAsync(
        Guid orderId,
        AddressDto deliveryAddress,
        IReadOnlyCollection<OrderItemDto> items,
        CancellationToken cancellationToken);

    Task CancelShipmentAsync(string shipmentId, CancellationToken cancellationToken);

    Task<ShipmentStatusDto> GetShipmentStatusAsync(
        string trackingNumber,
        CancellationToken cancellationToken);

    Task RegisterWebhookAsync(
        string shipmentId,
        string callbackUrl,
        string[] events,
        CancellationToken cancellationToken);

    Task<ReturnShipmentResultDto> CreateReturnShipmentAsync(
        Guid orderId,
        Guid customerId,
        List<OrderItemDto> items,
        CancellationToken cancellationToken);

    Task CancelReturnShipmentAsync(
        string returnShipmentId,
        string reason,
        CancellationToken cancellationToken);

    Task<ReturnShipmentStatusDto> GetReturnShipmentStatusAsync(
        string returnTrackingNumber,
        CancellationToken cancellationToken);
}

using Application.DTOs;
using Application.DTOs.ShipmentGateway;

namespace Application.Gateways;

// @todo: create document with xiaoping express public api
public interface IShippingGateway
{
    Task<ShipmentResultDto> CreateShipmentAsync(
        Guid orderId,
        AddressDto deliveryAddress,
        IReadOnlyCollection<OrderItemDto> items,
        CancellationToken cancellationToken
        );
    
    Task CancelShipmentAsync(
        string shipmentId,
        CancellationToken cancellationToken
    );
    
    Task<ShipmentStatusDto> GetShipmentStatusAsync(
        string trackingNumber,
        CancellationToken cancellationToken
        );
    

    Task RegisterWebhookAsync(
        string callbackUrl,
        CancellationToken cancellationToken
        );
    
    Task<ReturnShipmentResultDto> CreateReturnShipmentAsync(
        Guid returnRequestId,
        Guid orderId,
        string originalTrackingNumber,
        AddressDto pickupAddress,
        CancellationToken cancellationToken);

    Task<ReturnShipmentStatusDto> GetReturnShipmentStatusAsync(
        string returnTrackingNumber,
        CancellationToken cancellationToken);
}
using Application.Common.Enums;
using Application.DTOs;
using Application.DTOs.ShipmentGateway;

namespace Application.Gateways;

public interface IShippingGateway
{
    Task<ShipmentResultDto> CreateShipmentAsync(
        Guid orderId,
        AddressDto deliveryAddress,
        IReadOnlyCollection<OrderItemDto> items,
        ShippingCarrier carrier,
        CancellationToken cancellationToken
        );
    
    Task CancelShipmentAsync(
        string shipmentId,
        CancellationToken cancellationToken
    );
       Task CancelReturnShipmentAsync(
        string returnShipmentId,
        string reason,
        CancellationToken cancellationToken
    );

    Task RegisterWebhookAsync(
        string shipmentId,
        string callbackUrl,
        string[] events,
        CancellationToken cancellationToken
        );
    
    Task<ReturnShipmentResultDto> CreateReturnShipmentAsync(
        Guid orderId,
        Guid customerId,
        List<OrderItemDto> items,
        ShippingCarrier carrier,
        CancellationToken cancellationToken);

    Task<ReturnShipmentStatusDto> GetReturnShipmentStatusAsync(
        string returnTrackingNumber,
        CancellationToken cancellationToken);
}
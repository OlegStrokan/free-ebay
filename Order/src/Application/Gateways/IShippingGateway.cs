using Application.DTOs;

namespace Application.Gateways;

// @todo: create document with xiaoping express public api
public interface IShippingGateway
{
    Task<string> CreateShipmentAsync(
        Guid orderId,
        AddressDto deliveryAddress,
        List<OrderItemDto> items,
        CancellationToken cancellationToken
        );

    Task CancelShipmentAsync(
        string shipmentId,
        string reason,
        CancellationToken cancellationToken
    );
}
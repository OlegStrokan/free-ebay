using Application.Common.Attributes;
using Application.DTOs;

namespace Application.Gateways;

// @todo: create document with xiaoping express public api
public interface IShippingGateway
{
    [Retry(maxRetries: 5, delayMilliseconds: 1000, exponentialBackoff: true)]
    Task<string> CreateShipmentAsync(
        Guid orderId,
        AddressDto deliveryAddress,
        List<OrderItemDto> items,
        CancellationToken cancellationToken
        );

    [Retry(maxRetries:3, delayMilliseconds: 500)]
    Task CancelShipmentAsync(
        string shipmentId,
        string reason,
        CancellationToken cancellationToken
    );

    Task RegisterWebhookAsync(
        string shipmentId,
        string callbackUrl,
        string[] events,
        CancellationToken cancellationToken
        );
    
    Task<string> CreateReturnShipmentAsync(
        Guid orderId,
        Guid customerId,
        List<OrderItemDto> items,
        CancellationToken cancellationToken);

    Task CancelReturnShipmentAsync(
        string returnShipmentId,
        string reason,
        CancellationToken cancellationToken);

    Task<ReturnShipmentStatusDto> GetReturnShipmentStatusAsync(
        string returnShipmentId,
        CancellationToken cancellationToken);
}
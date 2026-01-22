using Application.Common.Attributes;
using Application.DTOs;

namespace Application.Gateways;

public interface IEmailGateway
{
    [Retry(maxRetries: 5, delayMilliseconds: 1000, exponentialBackoff: true)]
    Task SendOrderConfirmationAsync(
        Guid customerId,
        Guid orderId,
        decimal orderTotal,
        string currency,
        List<OrderItemDto> items,
        AddressDto deliveryDelivery,
        DateTime estimatedDelivery,
        CancellationToken cancellationToken);
}
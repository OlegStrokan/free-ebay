using Application.Common;
using Application.DTOs;

namespace Application.Gateways;

public interface IEmailGateway
{
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
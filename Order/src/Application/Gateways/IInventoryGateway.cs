using Application.Common.Attributes;

namespace Application.Gateways;

using Application.DTOs;

public interface IInventoryGateway
{
    [Retry(maxRetries: 5, delayMilliseconds: 1000, exponentialBackoff: true)]
    Task<string> ReserveAsync(Guid orderId, List<OrderItemDto> items, CancellationToken cancellationToken);
   
    [Retry(maxRetries:3, delayMilliseconds: 500)]
    Task ReleaseReservationAsync(string reservationId, CancellationToken cancellationToken);
}
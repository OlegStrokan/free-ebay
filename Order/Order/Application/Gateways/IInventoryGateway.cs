
using Application.DTOs;

namespace Application.Gateways;


public interface IInventoryGateway
{
    Task<string> ReserveAsync(Guid orderId, List<OrderItemDto> items, CancellationToken cancellationToken);

    Task ConfirmReservationAsync(string reservationId, CancellationToken cancellationToken);
   
    Task ReleaseReservationAsync(string reservationId, CancellationToken cancellationToken);
}
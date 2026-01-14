namespace Application.Gateways;

using Application.DTOs;

public interface IInventoryGateway
{
    Task<string> ReserveAsync(Guid orderId, List<OrderItemDto> items, CancellationToken cancellationToken);
    Task ReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken);
}
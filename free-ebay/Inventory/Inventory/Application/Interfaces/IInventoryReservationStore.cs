using Application.Models;

namespace Application.Interfaces;

public interface IInventoryReservationStore
{
    Task<ReserveInventoryResult> ReserveAsync(
        Guid orderId,
        IReadOnlyCollection<ReserveStockItem> items,
        CancellationToken cancellationToken);

    Task<ReleaseInventoryResult> ReleaseAsync(
        Guid reservationId,
        CancellationToken cancellationToken);
}

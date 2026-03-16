using Application.Models;

namespace Application.Interfaces;

public interface IInventoryService
{
    Task<ReserveInventoryResult> ReserveAsync(
        ReserveInventoryCommand command,
        CancellationToken cancellationToken);

    Task<ReleaseInventoryResult> ReleaseAsync(
        Guid reservationId,
        CancellationToken cancellationToken);
}

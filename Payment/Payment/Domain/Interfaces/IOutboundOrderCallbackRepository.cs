using Domain.Entities;

namespace Domain.Interfaces;

public interface IOutboundOrderCallbackRepository
{
    Task<OutboundOrderCallback?> GetByCallbackEventIdAsync(string callbackEventId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboundOrderCallback>> GetPendingAsync(
        DateTime now,
        int maxCount,
        CancellationToken cancellationToken = default);

    Task AddAsync(OutboundOrderCallback callback, CancellationToken cancellationToken = default);

    Task UpdateAsync(OutboundOrderCallback callback, CancellationToken cancellationToken = default);
}
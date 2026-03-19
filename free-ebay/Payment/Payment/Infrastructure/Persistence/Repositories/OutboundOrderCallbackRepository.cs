using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class OutboundOrderCallbackRepository(PaymentDbContext dbContext) : IOutboundOrderCallbackRepository
{
    public async Task<OutboundOrderCallback?> GetByCallbackEventIdAsync(
        string callbackEventId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.OutboundOrderCallbacks
            .FirstOrDefaultAsync(x => x.CallbackEventId == callbackEventId, cancellationToken);
    }

    public async Task<IReadOnlyList<OutboundOrderCallback>> GetPendingAsync(
        DateTime now,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        var take = maxCount <= 0 ? 100 : maxCount;

        return await dbContext.OutboundOrderCallbacks
            .Where(x =>
                (x.Status == CallbackDeliveryStatus.Pending
                 || x.Status == CallbackDeliveryStatus.Failed)
                && (x.NextRetryAt == null || x.NextRetryAt <= now))
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.NextRetryAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(OutboundOrderCallback callback, CancellationToken cancellationToken = default)
    {
        await dbContext.OutboundOrderCallbacks.AddAsync(callback, cancellationToken);
    }

    public Task UpdateAsync(OutboundOrderCallback callback, CancellationToken cancellationToken = default)
    {
        var entry = dbContext.Entry(callback);
        if (entry.State == EntityState.Detached)
        {
            dbContext.OutboundOrderCallbacks.Update(callback);
        }

        return Task.CompletedTask;
    }
}
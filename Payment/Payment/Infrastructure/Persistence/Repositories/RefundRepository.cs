using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class RefundRepository(PaymentDbContext dbContext) : IRefundRepository
{
    public async Task<Refund?> GetByIdAsync(RefundId refundId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Refunds
            .FirstOrDefaultAsync(x => x.Id == refundId, cancellationToken);
    }

    public async Task<Refund?> GetByProviderRefundIdAsync(
        ProviderRefundId providerRefundId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Refunds
            .FirstOrDefaultAsync(x => x.ProviderRefundId == providerRefundId, cancellationToken);
    }

    public async Task<Refund?> GetPendingByPaymentIdAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Refunds
            .Where(x =>
                x.PaymentId == paymentId
                && (x.Status == RefundStatus.Requested
                    || x.Status == RefundStatus.PendingProviderConfirmation))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Refund?> GetByPaymentIdAndIdempotencyKeyAsync(
        PaymentId paymentId,
        IdempotencyKey idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Refunds
            .FirstOrDefaultAsync(
                x => x.PaymentId == paymentId && x.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Refund>> GetPendingProviderConfirmationsOlderThanAsync(
        DateTime threshold,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        var take = maxCount <= 0 ? 100 : maxCount;

        return await dbContext.Refunds
            .Where(x => x.Status == RefundStatus.PendingProviderConfirmation && x.UpdatedAt <= threshold)
            .OrderBy(x => x.UpdatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        await dbContext.Refunds.AddAsync(refund, cancellationToken);
    }

    public Task UpdateAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        var entry = dbContext.Entry(refund);
        if (entry.State == EntityState.Detached)
        {
            dbContext.Refunds.Update(refund);
        }

        return Task.CompletedTask;
    }
}
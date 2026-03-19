using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class PaymentRepository(PaymentDbContext dbContext) : IPaymentRepository
{
    // @think: should i use paymentId string? because of PaymentId argument, i always need to cast this type from request.PaymentId
    public async Task<Payment?> GetByIdAsync(PaymentId paymentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Payments
            .FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);
    }

    public async Task<Payment?> GetByOrderIdAndIdempotencyKeyAsync(
        string orderId,
        IdempotencyKey idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Payments
            .FirstOrDefaultAsync(
                x => x.OrderId == orderId && x.ProcessIdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetPendingProviderConfirmationsOlderThanAsync(
        DateTime threshold,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        var take = maxCount <= 0 ? 100 : maxCount;

        return await dbContext.Payments
            .Where(x => x.Status == PaymentStatus.PendingProviderConfirmation && x.UpdatedAt <= threshold)
            .OrderBy(x => x.UpdatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await dbContext.Payments.AddAsync(payment, cancellationToken);
    }

    public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        var entry = dbContext.Entry(payment);
        if (entry.State == EntityState.Detached)
        {
            dbContext.Payments.Update(payment);
        }

        return Task.CompletedTask;
    }
}
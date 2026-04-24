using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Interfaces;

public interface IRefundRepository
{
    Task<Refund?> GetByIdAsync(RefundId refundId, CancellationToken cancellationToken = default);

    Task<Refund?> GetByProviderRefundIdAsync(
        ProviderRefundId providerRefundId,
        CancellationToken cancellationToken = default);

    Task<Refund?> GetPendingByPaymentIdAsync(PaymentId paymentId, CancellationToken cancellationToken = default);

    Task<Refund?> GetByPaymentIdAndIdempotencyKeyAsync(
        PaymentId paymentId,
        IdempotencyKey idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Refund>> GetPendingProviderConfirmationsOlderThanAsync(
        DateTime threshold,
        int maxCount,
        CancellationToken cancellationToken = default);

    Task AddAsync(Refund refund, CancellationToken cancellationToken = default);

    Task UpdateAsync(Refund refund, CancellationToken cancellationToken = default);
}
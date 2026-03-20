using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Services;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Refund : Entity<RefundId>
{
    private Refund()
    {
    }

    private Refund(
        RefundId id,
        PaymentId paymentId,
        Money amount,
        string reason,
        IdempotencyKey idempotencyKey,
        DateTime createdAt)
        : base(id)
    {
        PaymentId = paymentId;
        Amount = amount;
        Reason = reason;
        IdempotencyKey = idempotencyKey;
        Status = RefundStatus.Requested;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public PaymentId PaymentId { get; private set; } = PaymentId.From("unset");

    public Money Amount { get; private set; } = new(0, "USD");

    public string Reason { get; private set; } = string.Empty;

    public IdempotencyKey IdempotencyKey { get; private set; } = IdempotencyKey.From("unset");

    public ProviderRefundId? ProviderRefundId { get; private set; }

    public RefundStatus Status { get; private set; }

    public FailureReason? FailureReason { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public static Refund Create(
        PaymentId paymentId,
        Money amount,
        string reason,
        IdempotencyKey idempotencyKey,
        DateTime? createdAt = null)
    {
        if (!amount.IsGreaterThanZero())
        {
            throw new InvalidValueException("Refund amount must be greater than zero");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidValueException("Refund reason cannot be empty");
        }

        var now = createdAt ?? DateTime.UtcNow;
        return new Refund(RefundId.CreateUnique(), paymentId, amount, reason.Trim(), idempotencyKey, now);
    }

    public void MarkPendingProviderConfirmation(ProviderRefundId providerRefundId, DateTime? pendingAt = null)
    {
        RefundStateMachine.EnsureCanTransition(Status, RefundStatus.PendingProviderConfirmation);

        var now = pendingAt ?? DateTime.UtcNow;
        ProviderRefundId = providerRefundId;
        Status = RefundStatus.PendingProviderConfirmation;
        FailureReason = null;
        UpdatedAt = now;
    }

    public void MarkSucceeded(ProviderRefundId providerRefundId, DateTime? succeededAt = null)
    {
        RefundStateMachine.EnsureCanTransition(Status, RefundStatus.Succeeded);

        var now = succeededAt ?? DateTime.UtcNow;
        ProviderRefundId = providerRefundId;
        Status = RefundStatus.Succeeded;
        FailureReason = null;
        CompletedAt = now;
        UpdatedAt = now;
    }

    public void MarkFailed(FailureReason reason, DateTime? failedAt = null)
    {
        RefundStateMachine.EnsureCanTransition(Status, RefundStatus.Failed);

        var now = failedAt ?? DateTime.UtcNow;
        Status = RefundStatus.Failed;
        FailureReason = reason;
        UpdatedAt = now;
    }
}
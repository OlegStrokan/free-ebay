using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Tests.Entities;

public class PaymentTests
{
    private static readonly DateTime FixedNow = new(2026, 3, 24, 10, 0, 0, DateTimeKind.Utc);

    private static Payment CreateBasePayment() =>
        Payment.Create(
            PaymentId.From("pay-1"),
            "order-1",
            "customer-1",
            Money.Create(100m, "USD"),
            PaymentMethod.Card,
            IdempotencyKey.From("idem-1"),
            FixedNow);

    [Fact]
    public void Create_ShouldInitializeState_AndRaiseCreatedEvent()
    {
        var payment = CreateBasePayment();

        Assert.Equal("order-1", payment.OrderId);
        Assert.Equal("customer-1", payment.CustomerId);
        Assert.Equal(PaymentStatus.Created, payment.Status);
        Assert.Equal(100m, payment.Amount.Amount);
        Assert.Equal("USD", payment.Amount.Currency);
        Assert.Equal(FixedNow, payment.CreatedAt);
        Assert.Equal(FixedNow, payment.UpdatedAt);

        var createdEvent = Assert.IsType<PaymentCreatedEvent>(Assert.Single(payment.DomainEvents));
        Assert.Equal(payment.Id, createdEvent.PaymentId);
        Assert.Equal("order-1", createdEvent.OrderId);
    }

    [Fact]
    public void MarkPendingProviderConfirmation_ShouldSetStatusAndRaiseEvent()
    {
        var payment = CreateBasePayment();
        payment.ClearDomainEvents();

        payment.MarkPendingProviderConfirmation(ProviderPaymentIntentId.From("pi_123"), FixedNow.AddMinutes(1));

        Assert.Equal(PaymentStatus.PendingProviderConfirmation, payment.Status);
        Assert.Equal("pi_123", payment.ProviderPaymentIntentId?.Value);
        Assert.Equal(FixedNow.AddMinutes(1), payment.UpdatedAt);

        var pendingEvent = Assert.IsType<PaymentPendingProviderConfirmationEvent>(Assert.Single(payment.DomainEvents));
        Assert.Equal("pi_123", pendingEvent.ProviderPaymentIntentId.Value);
    }

    [Fact]
    public void StartRefund_ShouldThrow_WhenAmountExceedsOriginalPayment()
    {
        var payment = CreateBasePayment();
        payment.MarkSucceeded(ProviderPaymentIntentId.From("pi_123"), FixedNow.AddMinutes(1));

        var ex = Assert.Throws<DomainException>(() =>
            payment.StartRefund(
                RefundId.CreateUnique(),
                Money.Create(150m, "USD"),
                "requested_by_customer",
                FixedNow.AddMinutes(2)));

        Assert.Contains("cannot exceed original payment", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MarkSucceeded_ShouldSetSucceededFields_AndRaiseEvent()
    {
        var payment = CreateBasePayment();
        payment.ClearDomainEvents();

        payment.MarkSucceeded(ProviderPaymentIntentId.From("pi_777"), FixedNow.AddMinutes(3));

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal("pi_777", payment.ProviderPaymentIntentId?.Value);
        Assert.Equal(FixedNow.AddMinutes(3), payment.SucceededAt);
        Assert.Equal(FixedNow.AddMinutes(3), payment.UpdatedAt);
        Assert.Null(payment.FailureReason);

        var succeededEvent = Assert.IsType<PaymentSucceededEvent>(Assert.Single(payment.DomainEvents));
        Assert.Equal(payment.Id, succeededEvent.PaymentId);
    }
}
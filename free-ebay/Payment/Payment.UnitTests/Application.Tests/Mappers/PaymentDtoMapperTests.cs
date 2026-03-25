using Application.DTOs;
using Application.Mappers;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Tests.Mappers;

public class PaymentDtoMapperTests
{
    private static Payment CreateSucceededPayment()
    {
        var payment = Payment.Create(
            PaymentId.From("pay-1"),
            "order-1",
            "customer-1",
            Money.Create(120m, "USD"),
            PaymentMethod.Card,
            IdempotencyKey.From("idem-1"));

        payment.MarkSucceeded(ProviderPaymentIntentId.From("pi_1"));
        return payment;
    }

    [Fact]
    public void ToPaymentDetailsDto_ShouldMapAllFields()
    {
        var payment = CreateSucceededPayment();

        var dto = PaymentDtoMapper.ToPaymentDetailsDto(payment);

        Assert.Equal(payment.Id.Value, dto.PaymentId);
        Assert.Equal(payment.OrderId, dto.OrderId);
        Assert.Equal(payment.CustomerId, dto.CustomerId);
        Assert.Equal(payment.Amount.Amount, dto.Amount);
        Assert.Equal(payment.Amount.Currency, dto.Currency);
        Assert.Equal(payment.Method, dto.PaymentMethod);
        Assert.Equal(payment.Status, dto.Status);
        Assert.Equal("pi_1", dto.ProviderPaymentIntentId);
    }

    [Fact]
    public void ToProcessPaymentResult_ShouldPreferOverridesOverEntityFailure()
    {
        var payment = Payment.Create(
            PaymentId.From("pay-2"),
            "order-2",
            "customer-2",
            Money.Create(80m, "USD"),
            PaymentMethod.Card,
            IdempotencyKey.From("idem-2"));

        payment.MarkFailed(FailureReason.Create("DECLINED", "bank decline"));

        var dto = PaymentDtoMapper.ToProcessPaymentResult(
            payment,
            overrideStatus: ProcessPaymentStatus.RequiresAction,
            clientSecret: "cs_1",
            errorCode: "OVERRIDE",
            errorMessage: "override message");

        Assert.Equal(ProcessPaymentStatus.RequiresAction, dto.Status);
        Assert.Equal("cs_1", dto.ClientSecret);
        Assert.Equal("OVERRIDE", dto.ErrorCode);
        Assert.Equal("override message", dto.ErrorMessage);
    }

    [Fact]
    public void ToRefundPaymentResult_ShouldMapFromRefund()
    {
        var payment = CreateSucceededPayment();
        var refund = Refund.Create(
            payment.Id,
            Money.Create(20m, "USD"),
            "requested_by_customer",
            IdempotencyKey.From("ridem-1"));

        refund.MarkSucceeded(ProviderRefundId.From("re_1"));

        var dto = PaymentDtoMapper.ToRefundPaymentResult(payment, refund);

        Assert.Equal(payment.Id.Value, dto.PaymentId);
        Assert.Equal(refund.Id.Value, dto.RefundId);
        Assert.Equal(RefundPaymentStatus.Succeeded, dto.Status);
        Assert.Equal("re_1", dto.ProviderRefundId);
    }

    [Fact]
    public void ToProcessPaymentStatus_ShouldMapKnownStatuses()
    {
        Assert.Equal(ProcessPaymentStatus.Pending, PaymentDtoMapper.ToProcessPaymentStatus(PaymentStatus.Created));
        Assert.Equal(ProcessPaymentStatus.Pending, PaymentDtoMapper.ToProcessPaymentStatus(PaymentStatus.PendingProviderConfirmation));
        Assert.Equal(ProcessPaymentStatus.Succeeded, PaymentDtoMapper.ToProcessPaymentStatus(PaymentStatus.Succeeded));
        Assert.Equal(ProcessPaymentStatus.Failed, PaymentDtoMapper.ToProcessPaymentStatus(PaymentStatus.Failed));
        Assert.Equal(ProcessPaymentStatus.Succeeded, PaymentDtoMapper.ToProcessPaymentStatus(PaymentStatus.RefundPending));
        Assert.Equal(ProcessPaymentStatus.Succeeded, PaymentDtoMapper.ToProcessPaymentStatus(PaymentStatus.Refunded));
        Assert.Equal(ProcessPaymentStatus.Succeeded, PaymentDtoMapper.ToProcessPaymentStatus(PaymentStatus.RefundFailed));
    }

    [Fact]
    public void ToRefundPaymentStatus_ShouldMapKnownStatuses()
    {
        Assert.Equal(RefundPaymentStatus.Pending, PaymentDtoMapper.ToRefundPaymentStatus(RefundStatus.Requested));
        Assert.Equal(RefundPaymentStatus.Pending, PaymentDtoMapper.ToRefundPaymentStatus(RefundStatus.PendingProviderConfirmation));
        Assert.Equal(RefundPaymentStatus.Succeeded, PaymentDtoMapper.ToRefundPaymentStatus(RefundStatus.Succeeded));
        Assert.Equal(RefundPaymentStatus.Failed, PaymentDtoMapper.ToRefundPaymentStatus(RefundStatus.Failed));
    }
}

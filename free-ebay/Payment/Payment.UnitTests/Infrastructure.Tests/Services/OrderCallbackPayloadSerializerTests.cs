using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Services;
using NSubstitute;
using System.Text.Json;

namespace Infrastructure.Tests.Services;

public class OrderCallbackPayloadSerializerTests
{
    private static Payment CreatePayment()
    {
        var payment = Payment.Create(
            PaymentId.From("pay-1"),
            "order-1",
            "customer-1",
            Money.Create(100m, "USD"),
            PaymentMethod.Card,
            IdempotencyKey.From("idem-1"));

        payment.MarkSucceeded(ProviderPaymentIntentId.From("pi_1"));
        return payment;
    }

    private static Refund CreateRefund(Payment payment)
    {
        var refund = Refund.Create(
            payment.Id,
            Money.Create(20m, "USD"),
            "customer request",
            IdempotencyKey.From("ridem-1"));

        refund.MarkSucceeded(ProviderRefundId.From("re_1"));
        return refund;
    }

    [Fact]
    public void SerializePaymentSucceeded_ShouldContainExpectedPayloadFields()
    {
        var serializer = new OrderCallbackPayloadSerializer(Substitute.For<IClock>());
        var payment = CreatePayment();

        var json = serializer.SerializePaymentSucceeded("evt-1", payment);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(OrderCallbackEventTypes.PaymentSucceeded, root.GetProperty("eventType").GetString());
        Assert.Equal("evt-1", root.GetProperty("callbackEventId").GetString());
        Assert.Equal(payment.OrderId, root.GetProperty("orderId").GetString());
        Assert.Equal(payment.Id.Value, root.GetProperty("paymentId").GetString());
        Assert.Equal("pi_1", root.GetProperty("providerPaymentIntentId").GetString());
        Assert.True(root.TryGetProperty("occurredOn", out _));
    }

    [Fact]
    public void SerializePaymentFailed_ShouldIncludeErrorDetails()
    {
        var serializer = new OrderCallbackPayloadSerializer(Substitute.For<IClock>());
        var payment = CreatePayment();
        var reason = FailureReason.Create("DECLINED", "bank decline");

        var json = serializer.SerializePaymentFailed("evt-2", payment, reason);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(OrderCallbackEventTypes.PaymentFailed, root.GetProperty("eventType").GetString());
        Assert.Equal("DECLINED", root.GetProperty("errorCode").GetString());
        Assert.Equal("bank decline", root.GetProperty("errorMessage").GetString());
    }

    [Fact]
    public void SerializeRefundSucceeded_ShouldContainRefundIdentifiers()
    {
        var serializer = new OrderCallbackPayloadSerializer(Substitute.For<IClock>());
        var payment = CreatePayment();
        var refund = CreateRefund(payment);

        var json = serializer.SerializeRefundSucceeded("evt-3", payment, refund);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(OrderCallbackEventTypes.RefundSucceeded, root.GetProperty("eventType").GetString());
        Assert.Equal(refund.Id.Value, root.GetProperty("refundId").GetString());
        Assert.Equal("re_1", root.GetProperty("providerRefundId").GetString());
    }

    [Fact]
    public void SerializeRefundFailed_ShouldContainFailureFields()
    {
        var serializer = new OrderCallbackPayloadSerializer(Substitute.For<IClock>());
        var payment = CreatePayment();
        var refund = CreateRefund(payment);
        var reason = FailureReason.Create("RF_FAIL", "provider failed");

        var json = serializer.SerializeRefundFailed("evt-4", payment, refund, reason);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(OrderCallbackEventTypes.RefundFailed, root.GetProperty("eventType").GetString());
        Assert.Equal("RF_FAIL", root.GetProperty("errorCode").GetString());
        Assert.Equal("provider failed", root.GetProperty("errorMessage").GetString());
    }
}

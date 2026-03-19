using Application.Common;
using Application.Gateways;
using Domain.Entities;
using Domain.ValueObjects;
using System.Text.Json;

namespace Infrastructure.Services;

internal sealed class OrderCallbackPayloadSerializer : IOrderCallbackPayloadSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string SerializePaymentSucceeded(string callbackEventId, Payment payment)
    {
        var payload = new PaymentSucceededPayload(
            EventType: OrderCallbackEventTypes.PaymentSucceeded,
            CallbackEventId: callbackEventId,
            OrderId: payment.OrderId,
            PaymentId: payment.Id.Value,
            ProviderPaymentIntentId: payment.ProviderPaymentIntentId?.Value,
            OccurredOn: DateTime.UtcNow);

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    public string SerializePaymentFailed(string callbackEventId, Payment payment, FailureReason reason)
    {
        var payload = new PaymentFailedPayload(
            EventType: OrderCallbackEventTypes.PaymentFailed,
            CallbackEventId: callbackEventId,
            OrderId: payment.OrderId,
            PaymentId: payment.Id.Value,
            ProviderPaymentIntentId: payment.ProviderPaymentIntentId?.Value,
            ErrorCode: reason.Code,
            ErrorMessage: reason.Message,
            OccurredOn: DateTime.UtcNow);

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    public string SerializeRefundSucceeded(string callbackEventId, Payment payment, Refund refund)
    {
        var payload = new RefundSucceededPayload(
            EventType: OrderCallbackEventTypes.RefundSucceeded,
            CallbackEventId: callbackEventId,
            OrderId: payment.OrderId,
            PaymentId: payment.Id.Value,
            RefundId: refund.Id.Value,
            ProviderRefundId: refund.ProviderRefundId?.Value,
            OccurredOn: DateTime.UtcNow);

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    public string SerializeRefundFailed(string callbackEventId, Payment payment, Refund refund, FailureReason reason)
    {
        var payload = new RefundFailedPayload(
            EventType: OrderCallbackEventTypes.RefundFailed,
            CallbackEventId: callbackEventId,
            OrderId: payment.OrderId,
            PaymentId: payment.Id.Value,
            RefundId: refund.Id.Value,
            ProviderRefundId: refund.ProviderRefundId?.Value,
            ErrorCode: reason.Code,
            ErrorMessage: reason.Message,
            OccurredOn: DateTime.UtcNow);

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    private sealed record PaymentSucceededPayload(
        string EventType,
        string CallbackEventId,
        string OrderId,
        string PaymentId,
        string? ProviderPaymentIntentId,
        DateTime OccurredOn);

    private sealed record PaymentFailedPayload(
        string EventType,
        string CallbackEventId,
        string OrderId,
        string PaymentId,
        string? ProviderPaymentIntentId,
        string? ErrorCode,
        string ErrorMessage,
        DateTime OccurredOn);

    private sealed record RefundSucceededPayload(
        string EventType,
        string CallbackEventId,
        string OrderId,
        string PaymentId,
        string RefundId,
        string? ProviderRefundId,
        DateTime OccurredOn);

    private sealed record RefundFailedPayload(
        string EventType,
        string CallbackEventId,
        string OrderId,
        string PaymentId,
        string RefundId,
        string? ProviderRefundId,
        string? ErrorCode,
        string ErrorMessage,
        DateTime OccurredOn);
}
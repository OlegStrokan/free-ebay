using System.Text;
using System.Text.Json;
using Application.Common;
using Confluent.Kafka;
using Domain.Entities;
using Infrastructure.Messaging;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Callbacks;

internal sealed class OrderCallbackKafkaDispatcher : IOrderCallbackDispatcher, IDisposable
{
    private static readonly JsonSerializerOptions DeserializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _kafkaOptions;
    private readonly ILogger<OrderCallbackKafkaDispatcher> _logger;

    public OrderCallbackKafkaDispatcher(
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<OrderCallbackKafkaDispatcher> logger)
    {
        _logger = logger;
        _kafkaOptions = kafkaOptions.Value;

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            ClientId = string.IsNullOrWhiteSpace(_kafkaOptions.ProducerClientId)
                ? "payment-service"
                : _kafkaOptions.ProducerClientId,
            EnableIdempotence = true,
            MaxInFlight = 1,
            Acks = Acks.All,
            MessageSendMaxRetries = 10,
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task<CallbackDeliveryResult> DispatchAsync(
        OutboundOrderCallback callback,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_kafkaOptions.BootstrapServers))
        {
            return new CallbackDeliveryResult(false, "Kafka bootstrap servers are not configured.");
        }

        if (string.IsNullOrWhiteSpace(_kafkaOptions.SagaTopic))
        {
            return new CallbackDeliveryResult(false, "Kafka saga topic is not configured.");
        }

        try
        {
            var eventWrapper = BuildEventWrapper(callback);
            var envelopeJson = JsonSerializer.Serialize(eventWrapper);

            var message = new Message<string, string>
            {
                Key = callback.OrderId,
                Value = envelopeJson,
                Headers = new Headers
                {
                    { "event-type", Encoding.UTF8.GetBytes(eventWrapper.EventType) },
                    { "event-id", Encoding.UTF8.GetBytes(eventWrapper.EventId.ToString("D")) },
                },
            };

            var deliveryResult = await _producer.ProduceAsync(
                _kafkaOptions.SagaTopic,
                message,
                cancellationToken);

            _logger.LogInformation(
                "Published callback {CallbackEventId} as {EventType} to Kafka topic {Topic}. Partition={Partition}, Offset={Offset}",
                callback.CallbackEventId,
                callback.EventType,
                _kafkaOptions.SagaTopic,
                deliveryResult.Partition,
                deliveryResult.Offset);

            return new CallbackDeliveryResult(true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to publish callback {CallbackEventId} to Kafka topic {Topic}",
                callback.CallbackEventId,
                _kafkaOptions.SagaTopic);

            return new CallbackDeliveryResult(false, ex.Message);
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }

    private static EventWrapper BuildEventWrapper(OutboundOrderCallback callback)
    {
        var eventId = ParseEventId(callback.CallbackEventId);

        return callback.EventType switch
        {
            OrderCallbackEventTypes.PaymentSucceeded => BuildPaymentSucceededEvent(callback, eventId),
            OrderCallbackEventTypes.PaymentFailed => BuildPaymentFailedEvent(callback, eventId),
            OrderCallbackEventTypes.RefundSucceeded => BuildRefundSucceededEvent(callback, eventId),
            OrderCallbackEventTypes.RefundFailed => BuildRefundFailedEvent(callback, eventId),
            _ => new EventWrapper
            {
                EventId = eventId,
                EventType = callback.EventType,
                Payload = callback.PayloadJson,
                OccurredOn = callback.CreatedAt,
            },
        };
    }

    private static EventWrapper BuildPaymentSucceededEvent(OutboundOrderCallback callback, Guid eventId)
    {
        var payload = DeserializeOrThrow<PaymentSucceededCallbackPayload>(callback.PayloadJson, callback.EventType);
        var orderId = Required(payload.OrderId, "OrderId", callback.EventType);
        var paymentId = Required(payload.PaymentId, "PaymentId", callback.EventType);
        var callbackEventId = Required(payload.CallbackEventId, "CallbackEventId", callback.EventType);
        var occurredOn = NormalizeOccurredOn(payload.OccurredOn, callback.CreatedAt);

        var sagaPayload = new PaymentSucceededSagaPayload(
            OrderId: orderId,
            PaymentId: paymentId,
            ProviderPaymentIntentId: payload.ProviderPaymentIntentId,
            CallbackEventId: callbackEventId,
            OccurredOn: occurredOn);

        return new EventWrapper
        {
            EventId = eventId,
            EventType = callback.EventType,
            Payload = JsonSerializer.Serialize(sagaPayload),
            OccurredOn = occurredOn,
        };
    }

    private static EventWrapper BuildPaymentFailedEvent(OutboundOrderCallback callback, Guid eventId)
    {
        var payload = DeserializeOrThrow<PaymentFailedCallbackPayload>(callback.PayloadJson, callback.EventType);
        var orderId = Required(payload.OrderId, "OrderId", callback.EventType);
        var paymentId = Required(payload.PaymentId, "PaymentId", callback.EventType);
        var callbackEventId = Required(payload.CallbackEventId, "CallbackEventId", callback.EventType);
        var occurredOn = NormalizeOccurredOn(payload.OccurredOn, callback.CreatedAt);

        var sagaPayload = new PaymentFailedSagaPayload(
            OrderId: orderId,
            PaymentId: paymentId,
            ProviderPaymentIntentId: payload.ProviderPaymentIntentId,
            ErrorCode: payload.ErrorCode,
            ErrorMessage: payload.ErrorMessage,
            CallbackEventId: callbackEventId,
            OccurredOn: occurredOn);

        return new EventWrapper
        {
            EventId = eventId,
            EventType = callback.EventType,
            Payload = JsonSerializer.Serialize(sagaPayload),
            OccurredOn = occurredOn,
        };
    }

    private static EventWrapper BuildRefundSucceededEvent(OutboundOrderCallback callback, Guid eventId)
    {
        var payload = DeserializeOrThrow<RefundSucceededCallbackPayload>(callback.PayloadJson, callback.EventType);
        var orderId = Required(payload.OrderId, "OrderId", callback.EventType);
        var paymentId = Required(payload.PaymentId, "PaymentId", callback.EventType);
        var refundId = Required(payload.RefundId, "RefundId", callback.EventType);
        var callbackEventId = Required(payload.CallbackEventId, "CallbackEventId", callback.EventType);
        var occurredOn = NormalizeOccurredOn(payload.OccurredOn, callback.CreatedAt);

        var sagaPayload = new RefundSucceededSagaPayload(
            OrderId: orderId,
            PaymentId: paymentId,
            RefundId: refundId,
            ProviderRefundId: payload.ProviderRefundId,
            CallbackEventId: callbackEventId,
            OccurredOn: occurredOn);

        return new EventWrapper
        {
            EventId = eventId,
            EventType = callback.EventType,
            Payload = JsonSerializer.Serialize(sagaPayload),
            OccurredOn = occurredOn,
        };
    }

    private static EventWrapper BuildRefundFailedEvent(OutboundOrderCallback callback, Guid eventId)
    {
        var payload = DeserializeOrThrow<RefundFailedCallbackPayload>(callback.PayloadJson, callback.EventType);
        var orderId = Required(payload.OrderId, "OrderId", callback.EventType);
        var paymentId = Required(payload.PaymentId, "PaymentId", callback.EventType);
        var refundId = Required(payload.RefundId, "RefundId", callback.EventType);
        var callbackEventId = Required(payload.CallbackEventId, "CallbackEventId", callback.EventType);
        var occurredOn = NormalizeOccurredOn(payload.OccurredOn, callback.CreatedAt);

        var sagaPayload = new RefundFailedSagaPayload(
            OrderId: orderId,
            PaymentId: paymentId,
            RefundId: refundId,
            ProviderRefundId: payload.ProviderRefundId,
            ErrorCode: payload.ErrorCode,
            ErrorMessage: payload.ErrorMessage,
            CallbackEventId: callbackEventId,
            OccurredOn: occurredOn);

        return new EventWrapper
        {
            EventId = eventId,
            EventType = callback.EventType,
            Payload = JsonSerializer.Serialize(sagaPayload),
            OccurredOn = occurredOn,
        };
    }

    private static T DeserializeOrThrow<T>(string payloadJson, string eventType)
    {
        var payload = JsonSerializer.Deserialize<T>(payloadJson, DeserializerOptions);
        if (payload is not null)
        {
            return payload;
        }

        throw new InvalidOperationException($"Callback payload for event '{eventType}' is invalid.");
    }

    private static string Required(string? value, string fieldName, string eventType)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException(
            $"Callback payload for event '{eventType}' is missing required field '{fieldName}'.");
    }

    private static DateTime NormalizeOccurredOn(DateTime? value, DateTime fallback)
    {
        if (value.HasValue && value.Value != default)
        {
            return value.Value;
        }

        return fallback;
    }

    private static Guid ParseEventId(string callbackEventId)
    {
        return Guid.TryParse(callbackEventId, out var parsed)
            ? parsed
            : Guid.NewGuid();
    }

    private sealed record PaymentSucceededCallbackPayload(
        string? EventType,
        string? CallbackEventId,
        string? OrderId,
        string? PaymentId,
        string? ProviderPaymentIntentId,
        DateTime? OccurredOn);

    private sealed record PaymentFailedCallbackPayload(
        string? EventType,
        string? CallbackEventId,
        string? OrderId,
        string? PaymentId,
        string? ProviderPaymentIntentId,
        string? ErrorCode,
        string? ErrorMessage,
        DateTime? OccurredOn);

    private sealed record RefundSucceededCallbackPayload(
        string? EventType,
        string? CallbackEventId,
        string? OrderId,
        string? PaymentId,
        string? RefundId,
        string? ProviderRefundId,
        DateTime? OccurredOn);

    private sealed record RefundFailedCallbackPayload(
        string? EventType,
        string? CallbackEventId,
        string? OrderId,
        string? PaymentId,
        string? RefundId,
        string? ProviderRefundId,
        string? ErrorCode,
        string? ErrorMessage,
        DateTime? OccurredOn);

    private sealed record PaymentSucceededSagaPayload(
        string OrderId,
        string PaymentId,
        string? ProviderPaymentIntentId,
        string CallbackEventId,
        DateTime OccurredOn);

    private sealed record PaymentFailedSagaPayload(
        string OrderId,
        string PaymentId,
        string? ProviderPaymentIntentId,
        string? ErrorCode,
        string? ErrorMessage,
        string CallbackEventId,
        DateTime OccurredOn);

    private sealed record RefundSucceededSagaPayload(
        string OrderId,
        string PaymentId,
        string RefundId,
        string? ProviderRefundId,
        string CallbackEventId,
        DateTime OccurredOn);

    private sealed record RefundFailedSagaPayload(
        string OrderId,
        string PaymentId,
        string RefundId,
        string? ProviderRefundId,
        string? ErrorCode,
        string? ErrorMessage,
        string CallbackEventId,
        DateTime OccurredOn);
}
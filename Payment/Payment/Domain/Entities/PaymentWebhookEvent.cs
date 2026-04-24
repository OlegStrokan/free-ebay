using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public sealed class PaymentWebhookEvent : Entity<Guid>
{
    private PaymentWebhookEvent()
    {
    }

    private PaymentWebhookEvent(
        Guid id,
        string providerEventId,
        string eventType,
        string payloadJson,
        DateTime receivedAt)
        : base(id)
    {
        ProviderEventId = providerEventId;
        EventType = eventType;
        PayloadJson = payloadJson;
        ProcessingStatus = WebhookProcessingStatus.Received;
        ReceivedAt = receivedAt;
    }

    public string ProviderEventId { get; private set; } = string.Empty;

    public string EventType { get; private set; } = string.Empty;

    public string PayloadJson { get; private set; } = string.Empty;

    public WebhookProcessingStatus ProcessingStatus { get; private set; }

    public DateTime ReceivedAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public string? ProcessingError { get; private set; }

    public static PaymentWebhookEvent Create(
        string providerEventId,
        string eventType,
        string payloadJson,
        DateTime? receivedAt = null)
    {
        if (string.IsNullOrWhiteSpace(providerEventId))
        {
            throw new InvalidValueException("Provider event id cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new InvalidValueException("Webhook event type cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            throw new InvalidValueException("Webhook payload cannot be empty");
        }

        var now = receivedAt ?? DateTime.UtcNow;
        return new PaymentWebhookEvent(Guid.NewGuid(), providerEventId.Trim(), eventType.Trim(), payloadJson, now);
    }

    public void MarkProcessed(DateTime? processedAt = null)
    {
        ProcessingStatus = WebhookProcessingStatus.Processed;
        ProcessedAt = processedAt ?? DateTime.UtcNow;
        ProcessingError = null;
    }

    public void MarkIgnoredDuplicate(DateTime? processedAt = null)
    {
        ProcessingStatus = WebhookProcessingStatus.IgnoredDuplicate;
        ProcessedAt = processedAt ?? DateTime.UtcNow;
        ProcessingError = null;
    }

    public void MarkFailed(string error, DateTime? processedAt = null)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidValueException("Processing error cannot be empty");
        }

        ProcessingStatus = WebhookProcessingStatus.Failed;
        ProcessedAt = processedAt ?? DateTime.UtcNow;
        ProcessingError = error.Trim();
    }
}
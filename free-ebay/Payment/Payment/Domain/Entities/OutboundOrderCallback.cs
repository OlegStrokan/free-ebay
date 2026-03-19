using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

// will be send to order service back
public sealed class OutboundOrderCallback : Entity<Guid>
{
    private OutboundOrderCallback()
    {
    }

    private OutboundOrderCallback(
        Guid id,
        string callbackEventId,
        string orderId,
        string eventType,
        string payloadJson,
        DateTime createdAt)
        : base(id)
    {
        CallbackEventId = callbackEventId;
        OrderId = orderId;
        EventType = eventType;
        PayloadJson = payloadJson;
        Status = CallbackDeliveryStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public string CallbackEventId { get; private set; } = string.Empty;

    public string OrderId { get; private set; } = string.Empty;

    public string EventType { get; private set; } = string.Empty;

    public string PayloadJson { get; private set; } = string.Empty;

    public CallbackDeliveryStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public DateTime? LastAttemptAt { get; private set; }

    public DateTime? NextRetryAt { get; private set; }

    public string? LastError { get; private set; }

    public static OutboundOrderCallback Create(
        string callbackEventId,
        string orderId,
        string eventType,
        string payloadJson,
        DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(callbackEventId))
        {
            throw new InvalidValueException("Callback event id cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new InvalidValueException("Order id cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new InvalidValueException("Callback event type cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            throw new InvalidValueException("Callback payload cannot be empty");
        }

        var now = createdAt ?? DateTime.UtcNow;
        return new OutboundOrderCallback(
            Guid.NewGuid(),
            callbackEventId.Trim(),
            orderId.Trim(),
            eventType.Trim(),
            payloadJson,
            now);
    }

    public bool CanAttempt(DateTime now)
    {
        if (Status is CallbackDeliveryStatus.Delivered or CallbackDeliveryStatus.PermanentFailure)
        {
            return false;
        }

        return NextRetryAt is null || NextRetryAt <= now;
    }

    public void MarkDelivered(DateTime? deliveredAt = null)
    {
        var now = deliveredAt ?? DateTime.UtcNow;
        Status = CallbackDeliveryStatus.Delivered;
        LastAttemptAt = now;
        UpdatedAt = now;
        NextRetryAt = null;
        LastError = null;
        AttemptCount++;
    }

    public void MarkAttemptFailed(string error, DateTime nextRetryAt, DateTime? attemptedAt = null)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidValueException("Callback failure reason cannot be empty.");
        }

        var now = attemptedAt ?? DateTime.UtcNow;
        Status = CallbackDeliveryStatus.Failed;
        LastAttemptAt = now;
        UpdatedAt = now;
        NextRetryAt = nextRetryAt;
        LastError = error.Trim();
        AttemptCount++;
    }

    public void MarkPermanentFailure(string error, DateTime? attemptedAt = null)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidValueException("Callback failure reason cannot be empty.");
        }

        var now = attemptedAt ?? DateTime.UtcNow;
        Status = CallbackDeliveryStatus.PermanentFailure;
        LastAttemptAt = now;
        UpdatedAt = now;
        NextRetryAt = null;
        LastError = error.Trim();
        AttemptCount++;
    }
}
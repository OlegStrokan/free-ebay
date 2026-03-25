using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Tests.Entities;

public class OutboundOrderCallbackTests
{
    private static OutboundOrderCallback CreateCallback(
        string callbackEventId = "evt-001",
        string orderId = "order-123",
        string eventType = "payment.succeeded",
        string payloadJson = "{\"status\":\"succeeded\"}",
        DateTime? createdAt = null) =>
        OutboundOrderCallback.Create(callbackEventId, orderId, eventType, payloadJson, createdAt);

    #region Create

    [Fact]
    public void Create_ShouldSetStatusToPending()
    {
        var callback = CreateCallback();

        Assert.Equal(CallbackDeliveryStatus.Pending, callback.Status);
    }

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var createdAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var callback = CreateCallback(createdAt: createdAt);

        Assert.Equal("evt-001", callback.CallbackEventId);
        Assert.Equal("order-123", callback.OrderId);
        Assert.Equal("payment.succeeded", callback.EventType);
        Assert.Equal("{\"status\":\"succeeded\"}", callback.PayloadJson);
        Assert.Equal(createdAt, callback.CreatedAt);
        Assert.Equal(createdAt, callback.UpdatedAt);
        Assert.Equal(0, callback.AttemptCount);
        Assert.Null(callback.LastAttemptAt);
        Assert.Null(callback.NextRetryAt);
        Assert.Null(callback.LastError);
    }

    [Fact]
    public void Create_ShouldTrimStringFields()
    {
        var callback = OutboundOrderCallback.Create("  evt-001  ", "  order-123  ", "  payment.succeeded  ", "{}");

        Assert.Equal("evt-001", callback.CallbackEventId);
        Assert.Equal("order-123", callback.OrderId);
        Assert.Equal("payment.succeeded", callback.EventType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCallbackEventId_ShouldThrowInvalidValueException(string id)
    {
        var ex = Assert.Throws<InvalidValueException>(() => CreateCallback(callbackEventId: id));

        Assert.Contains("Callback event id cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrderId_ShouldThrowInvalidValueException(string orderId)
    {
        var ex = Assert.Throws<InvalidValueException>(() => CreateCallback(orderId: orderId));

        Assert.Contains("Order id cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyEventType_ShouldThrowInvalidValueException(string eventType)
    {
        var ex = Assert.Throws<InvalidValueException>(() => CreateCallback(eventType: eventType));

        Assert.Contains("Callback event type cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyPayloadJson_ShouldThrowInvalidValueException(string payload)
    {
        var ex = Assert.Throws<InvalidValueException>(() => CreateCallback(payloadJson: payload));

        Assert.Contains("Callback payload cannot be empty", ex.Message);
    }

    #endregion

    #region CanAttempt

    [Fact]
    public void CanAttempt_WhenPendingAndNoNextRetry_ShouldReturnTrue()
    {
        var callback = CreateCallback();

        Assert.True(callback.CanAttempt(DateTime.UtcNow));
    }

    [Fact]
    public void CanAttempt_WhenDelivered_ShouldReturnFalse()
    {
        var callback = CreateCallback();
        callback.MarkDelivered();

        Assert.False(callback.CanAttempt(DateTime.UtcNow));
    }

    [Fact]
    public void CanAttempt_WhenPermanentFailure_ShouldReturnFalse()
    {
        var callback = CreateCallback();
        callback.MarkPermanentFailure("too many failures");

        Assert.False(callback.CanAttempt(DateTime.UtcNow));
    }

    [Fact]
    public void CanAttempt_WhenNextRetryAtIsInFuture_ShouldReturnFalse()
    {
        var callback = CreateCallback();
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        callback.MarkAttemptFailed("network error", now.AddMinutes(5), now);

        Assert.False(callback.CanAttempt(now));
    }

    [Fact]
    public void CanAttempt_WhenNextRetryAtIsNow_ShouldReturnTrue()
    {
        var callback = CreateCallback();
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        callback.MarkAttemptFailed("error", now, now.AddMinutes(-1));

        Assert.True(callback.CanAttempt(now));
    }

    #endregion

    #region MarkDelivered

    [Fact]
    public void MarkDelivered_ShouldChangeStatusToDelivered()
    {
        var callback = CreateCallback();

        callback.MarkDelivered();

        Assert.Equal(CallbackDeliveryStatus.Delivered, callback.Status);
    }

    [Fact]
    public void MarkDelivered_ShouldIncrementAttemptCount()
    {
        var callback = CreateCallback();

        callback.MarkDelivered();

        Assert.Equal(1, callback.AttemptCount);
    }

    [Fact]
    public void MarkDelivered_ShouldClearNextRetryAtAndLastError()
    {
        var callback = CreateCallback();
        var now = DateTime.UtcNow;
        callback.MarkAttemptFailed("transient error", now.AddMinutes(1), now);

        callback.MarkDelivered();

        Assert.Null(callback.NextRetryAt);
        Assert.Null(callback.LastError);
    }

    [Fact]
    public void MarkDelivered_ShouldSetLastAttemptAt()
    {
        var callback = CreateCallback();
        var deliveredAt = new DateTime(2026, 1, 1, 15, 0, 0, DateTimeKind.Utc);

        callback.MarkDelivered(deliveredAt);

        Assert.Equal(deliveredAt, callback.LastAttemptAt);
    }

    #endregion

    #region MarkAttemptFailed

    [Fact]
    public void MarkAttemptFailed_ShouldChangeStatusToFailed()
    {
        var callback = CreateCallback();

        callback.MarkAttemptFailed("connection timeout", DateTime.UtcNow.AddMinutes(5));

        Assert.Equal(CallbackDeliveryStatus.Failed, callback.Status);
    }

    [Fact]
    public void MarkAttemptFailed_ShouldSetNextRetryAt()
    {
        var callback = CreateCallback();
        var nextRetry = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc);

        callback.MarkAttemptFailed("error", nextRetry);

        Assert.Equal(nextRetry, callback.NextRetryAt);
    }

    [Fact]
    public void MarkAttemptFailed_ShouldSetLastError()
    {
        var callback = CreateCallback();

        callback.MarkAttemptFailed("  connection timeout  ", DateTime.UtcNow.AddMinutes(5));

        Assert.Equal("connection timeout", callback.LastError);
    }

    [Fact]
    public void MarkAttemptFailed_ShouldIncrementAttemptCount()
    {
        var callback = CreateCallback();

        callback.MarkAttemptFailed("error", DateTime.UtcNow.AddMinutes(1));
        callback.MarkAttemptFailed("error2", DateTime.UtcNow.AddMinutes(2));

        Assert.Equal(2, callback.AttemptCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkAttemptFailed_WithEmptyError_ShouldThrowInvalidValueException(string error)
    {
        var callback = CreateCallback();

        Assert.Throws<InvalidValueException>(() =>
            callback.MarkAttemptFailed(error, DateTime.UtcNow.AddMinutes(5)));
    }

    #endregion

    #region MarkPermanentFailure

    [Fact]
    public void MarkPermanentFailure_ShouldChangeStatusToPermanentFailure()
    {
        var callback = CreateCallback();

        callback.MarkPermanentFailure("exhausted all retries");

        Assert.Equal(CallbackDeliveryStatus.PermanentFailure, callback.Status);
    }

    [Fact]
    public void MarkPermanentFailure_ShouldSetLastError()
    {
        var callback = CreateCallback();

        callback.MarkPermanentFailure("  exhausted all retries  ");

        Assert.Equal("exhausted all retries", callback.LastError);
    }

    [Fact]
    public void MarkPermanentFailure_ShouldClearNextRetryAt()
    {
        var callback = CreateCallback();
        callback.MarkAttemptFailed("error", DateTime.UtcNow.AddMinutes(5));

        callback.MarkPermanentFailure("giving up");

        Assert.Null(callback.NextRetryAt);
    }

    [Fact]
    public void MarkPermanentFailure_ShouldIncrementAttemptCount()
    {
        var callback = CreateCallback();

        callback.MarkPermanentFailure("giving up");

        Assert.Equal(1, callback.AttemptCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkPermanentFailure_WithEmptyError_ShouldThrowInvalidValueException(string error)
    {
        var callback = CreateCallback();

        Assert.Throws<InvalidValueException>(() => callback.MarkPermanentFailure(error));
    }

    #endregion
}

public class PaymentWebhookEventTests
{
    private static PaymentWebhookEvent CreateWebhookEvent(
        string providerEventId = "evt_stripe_001",
        string eventType = "payment_intent.succeeded",
        string payloadJson = "{\"id\":\"evt_001\"}",
        DateTime? receivedAt = null) =>
        PaymentWebhookEvent.Create(providerEventId, eventType, payloadJson, receivedAt);

    #region Create

    [Fact]
    public void Create_ShouldSetStatusToReceived()
    {
        var evt = CreateWebhookEvent();

        Assert.Equal(WebhookProcessingStatus.Received, evt.ProcessingStatus);
    }

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var receivedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var evt = CreateWebhookEvent(receivedAt: receivedAt);

        Assert.Equal("evt_stripe_001", evt.ProviderEventId);
        Assert.Equal("payment_intent.succeeded", evt.EventType);
        Assert.Equal("{\"id\":\"evt_001\"}", evt.PayloadJson);
        Assert.Equal(receivedAt, evt.ReceivedAt);
        Assert.Null(evt.ProcessedAt);
        Assert.Null(evt.ProcessingError);
    }

    [Fact]
    public void Create_ShouldTrimProviderEventIdAndEventType()
    {
        var evt = PaymentWebhookEvent.Create("  evt_001  ", "  payment.succeeded  ", "{}");

        Assert.Equal("evt_001", evt.ProviderEventId);
        Assert.Equal("payment.succeeded", evt.EventType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyProviderEventId_ShouldThrowInvalidValueException(string id)
    {
        var ex = Assert.Throws<InvalidValueException>(() => CreateWebhookEvent(providerEventId: id));

        Assert.Contains("Provider event id cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyEventType_ShouldThrowInvalidValueException(string eventType)
    {
        var ex = Assert.Throws<InvalidValueException>(() => CreateWebhookEvent(eventType: eventType));

        Assert.Contains("Webhook event type cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyPayloadJson_ShouldThrowInvalidValueException(string payload)
    {
        var ex = Assert.Throws<InvalidValueException>(() => CreateWebhookEvent(payloadJson: payload));

        Assert.Contains("Webhook payload cannot be empty", ex.Message);
    }

    [Fact]
    public void Create_IdShouldBeUnique()
    {
        var e1 = CreateWebhookEvent();
        var e2 = CreateWebhookEvent();

        Assert.NotEqual(e1.Id, e2.Id);
    }

    #endregion

    #region MarkProcessed

    [Fact]
    public void MarkProcessed_ShouldChangeStatusToProcessed()
    {
        var evt = CreateWebhookEvent();

        evt.MarkProcessed();

        Assert.Equal(WebhookProcessingStatus.Processed, evt.ProcessingStatus);
    }

    [Fact]
    public void MarkProcessed_ShouldSetProcessedAt()
    {
        var processedAt = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc);
        var evt = CreateWebhookEvent();

        evt.MarkProcessed(processedAt);

        Assert.Equal(processedAt, evt.ProcessedAt);
    }

    [Fact]
    public void MarkProcessed_ShouldClearProcessingError()
    {
        var evt = CreateWebhookEvent();
        evt.MarkFailed("some earlier failure");

        evt.MarkProcessed();

        Assert.Null(evt.ProcessingError);
    }

    #endregion

    #region MarkIgnoredDuplicate

    [Fact]
    public void MarkIgnoredDuplicate_ShouldChangeStatusToIgnoredDuplicate()
    {
        var evt = CreateWebhookEvent();

        evt.MarkIgnoredDuplicate();

        Assert.Equal(WebhookProcessingStatus.IgnoredDuplicate, evt.ProcessingStatus);
    }

    [Fact]
    public void MarkIgnoredDuplicate_ShouldSetProcessedAt()
    {
        var processedAt = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc);
        var evt = CreateWebhookEvent();

        evt.MarkIgnoredDuplicate(processedAt);

        Assert.Equal(processedAt, evt.ProcessedAt);
    }

    [Fact]
    public void MarkIgnoredDuplicate_ShouldClearProcessingError()
    {
        var evt = CreateWebhookEvent();
        evt.MarkFailed("previous failure");

        evt.MarkIgnoredDuplicate();

        Assert.Null(evt.ProcessingError);
    }

    #endregion

    #region MarkFailed

    [Fact]
    public void MarkFailed_ShouldChangeStatusToFailed()
    {
        var evt = CreateWebhookEvent();

        evt.MarkFailed("deserialization error");

        Assert.Equal(WebhookProcessingStatus.Failed, evt.ProcessingStatus);
    }

    [Fact]
    public void MarkFailed_ShouldSetProcessingError()
    {
        var evt = CreateWebhookEvent();

        evt.MarkFailed("  deserialization error  ");

        Assert.Equal("deserialization error", evt.ProcessingError);
    }

    [Fact]
    public void MarkFailed_ShouldSetProcessedAt()
    {
        var processedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var evt = CreateWebhookEvent();

        evt.MarkFailed("error", processedAt);

        Assert.Equal(processedAt, evt.ProcessedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkFailed_WithEmptyError_ShouldThrowInvalidValueException(string error)
    {
        var evt = CreateWebhookEvent();

        var ex = Assert.Throws<InvalidValueException>(() => evt.MarkFailed(error));

        Assert.Contains("Processing error cannot be empty", ex.Message);
    }

    #endregion
}

using Application.Models;

namespace Infrastructure.Tests;

public class KafkaRetryRecordTests
{
    private static KafkaRetryRecord CreateRecord(
        Guid? eventId = null,
        string eventType = "OrderCreated",
        int offset = 1,
        DateTime? nextRetryAt = null) =>
        KafkaRetryRecord.Create(
            eventId: eventId ?? Guid.NewGuid(),
            eventType: eventType,
            topic: "order.events",
            partition: 0,
            offset: offset,
            messageKey: "agg-1",
            payload: "{\"EventId\":\"00000000-0000-0000-0000-000000000001\"}",
            headers: null,
            correlationId: "trace-123",
            errorMessage: "initial error",
            errorType: "System.Exception",
            nextRetryAt: nextRetryAt ?? DateTime.UtcNow.AddMinutes(3));

    // ── Create ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldSetAllFields_Correctly()
    {
        var eventId = Guid.NewGuid();
        var nextRetry = DateTime.UtcNow.AddMinutes(5);
        var record = KafkaRetryRecord.Create(
            eventId, "OrderCreated", "order.events",
            1, 42, "key-1", "payload", "headers-json",
            "trace-abc", "error msg", "System.Exception", nextRetry);

        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal(eventId, record.EventId);
        Assert.Equal("OrderCreated", record.EventType);
        Assert.Equal("order.events", record.Topic);
        Assert.Equal(1, record.Partition);
        Assert.Equal(42L, record.Offset);
        Assert.Equal("key-1", record.MessageKey);
        Assert.Equal("payload", record.Payload);
        Assert.Equal("headers-json", record.Headers);
        Assert.Equal("trace-abc", record.CorrelationId);
        Assert.Equal("error msg", record.LastErrorMessage);
        Assert.Equal("System.Exception", record.LastErrorType);
        Assert.Equal(nextRetry, record.NextRetryAt);
        Assert.Equal(0, record.RetryCount);
        Assert.Equal(KafkaRetryRecordStatus.Pending, record.Status);
    }

    [Fact]
    public void Create_ShouldAssignUniqueId_EachTime()
    {
        var r1 = CreateRecord();
        var r2 = CreateRecord();
        Assert.NotEqual(r1.Id, r2.Id);
    }

    [Fact]
    public void Create_ShouldAllowNullEventId()
    {
        var record = KafkaRetryRecord.Create(
            null, "OrderCreated", "order.events",
            0, 0, null, "{}", null, null, null, null,
            DateTime.UtcNow.AddMinutes(3));

        Assert.Null(record.EventId);
    }

    // ── MarkInProgress ────────────────────────────────────────────────────

    [Fact]
    public void MarkInProgress_ShouldChangeStatus_ToInProgress()
    {
        var record = CreateRecord();
        record.MarkInProgress();
        Assert.Equal(KafkaRetryRecordStatus.InProgress, record.Status);
    }

    // ── MarkSucceeded ─────────────────────────────────────────────────────

    [Fact]
    public void MarkSucceeded_ShouldChangeStatus_ToSucceeded()
    {
        var record = CreateRecord();
        record.MarkInProgress();
        record.MarkSucceeded();
        Assert.Equal(KafkaRetryRecordStatus.Succeeded, record.Status);
    }

    // ── Reschedule ────────────────────────────────────────────────────────

    [Fact]
    public void Reschedule_ShouldUpdateCountNextRetryAndError()
    {
        var record = CreateRecord();
        var newNext = DateTime.UtcNow.AddMinutes(10);

        record.Reschedule(2, newNext, "new error", "System.InvalidOperationException");

        Assert.Equal(KafkaRetryRecordStatus.Pending, record.Status);
        Assert.Equal(2, record.RetryCount);
        Assert.Equal(newNext, record.NextRetryAt);
        Assert.Equal("new error", record.LastErrorMessage);
        Assert.Equal("System.InvalidOperationException", record.LastErrorType);
    }

    [Fact]
    public void Reschedule_ShouldUpdateLastFailureTime()
    {
        var record = CreateRecord();
        var before = DateTime.UtcNow;

        record.Reschedule(1, DateTime.UtcNow.AddMinutes(5), null, null);

        Assert.True(record.LastFailureTime >= before);
    }

    // ── MarkDeadLetter ────────────────────────────────────────────────────

    [Fact]
    public void MarkDeadLetter_ShouldChangeStatus_ToDeadLetter()
    {
        var record = CreateRecord();
        record.MarkDeadLetter("fatal error", "System.Exception");
        Assert.Equal(KafkaRetryRecordStatus.DeadLetter, record.Status);
    }

    [Fact]
    public void MarkDeadLetter_ShouldPreserveOriginalFirstFailureTime()
    {
        var record = CreateRecord();
        var originalFirst = record.FirstFailureTime;

        record.MarkDeadLetter("fatal", null);

        Assert.Equal(originalFirst, record.FirstFailureTime);
    }

    // ── State machine transitions ─────────────────────────────────────────

    [Fact]
    public void FullLifecycle_PendingToInProgressToSucceeded()
    {
        var record = CreateRecord();

        Assert.Equal(KafkaRetryRecordStatus.Pending, record.Status);

        record.MarkInProgress();
        Assert.Equal(KafkaRetryRecordStatus.InProgress, record.Status);

        record.MarkSucceeded();
        Assert.Equal(KafkaRetryRecordStatus.Succeeded, record.Status);
    }

    [Fact]
    public void FullLifecycle_PendingToInProgressToDeadLetter()
    {
        var record = CreateRecord();
        record.MarkInProgress();
        record.MarkDeadLetter("gave up", "System.Exception");
        Assert.Equal(KafkaRetryRecordStatus.DeadLetter, record.Status);
    }
}

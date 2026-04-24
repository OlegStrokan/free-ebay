namespace Application.Models;

public enum KafkaRetryRecordStatus
{
    Pending = 0,
    InProgress = 1,
    Succeeded = 2,
    DeadLetter = 3,
}

public sealed class KafkaRetryRecord
{
    public Guid Id { get; private set; }
    public Guid? EventId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Topic { get; private set; } = string.Empty;
    public int Partition { get; private set; }
    public long Offset { get; private set; }
    public string? MessageKey { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public string? Headers { get; private set; }
    public DateTime FirstFailureTime { get; private set; }
    public DateTime LastFailureTime { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime NextRetryAt { get; private set; }
    public KafkaRetryRecordStatus Status { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public string? LastErrorType { get; private set; }
    public string? CorrelationId { get; private set; }

    private KafkaRetryRecord() { }

    public static KafkaRetryRecord Create(
        Guid? eventId,
        string eventType,
        string topic,
        int partition,
        long offset,
        string? messageKey,
        string payload,
        string? headers,
        string? correlationId,
        string? errorMessage,
        string? errorType,
        DateTime nextRetryAt)
    {
        var now = DateTime.UtcNow;
        return new KafkaRetryRecord
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventType = eventType,
            Topic = topic,
            Partition = partition,
            Offset = offset,
            MessageKey = messageKey,
            Payload = payload,
            Headers = headers,
            FirstFailureTime = now,
            LastFailureTime = now,
            RetryCount = 0,
            NextRetryAt = nextRetryAt,
            Status = KafkaRetryRecordStatus.Pending,
            LastErrorMessage = errorMessage,
            LastErrorType = errorType,
            CorrelationId = correlationId,
        };
    }

    public void MarkInProgress() => Status = KafkaRetryRecordStatus.InProgress;

    public void MarkSucceeded() => Status = KafkaRetryRecordStatus.Succeeded;

    public void Reschedule(int newRetryCount, DateTime nextRetryAt, string? errorMessage, string? errorType)
    {
        Status = KafkaRetryRecordStatus.Pending;
        RetryCount = newRetryCount;
        NextRetryAt = nextRetryAt;
        LastFailureTime = DateTime.UtcNow;
        LastErrorMessage = errorMessage;
        LastErrorType = errorType;
    }

    public void MarkDeadLetter(string? errorMessage, string? errorType)
    {
        Status = KafkaRetryRecordStatus.DeadLetter;
        LastFailureTime = DateTime.UtcNow;
        LastErrorMessage = errorMessage;
        LastErrorType = errorType;
    }
}

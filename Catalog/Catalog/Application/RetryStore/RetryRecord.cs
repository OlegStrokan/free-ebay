namespace Application.RetryStore;

public sealed class RetryRecord
{
    public Guid Id { get; set; }
    public Guid? EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public int Partition { get; set; }
    public long Offset { get; set; }
    public string? MessageKey { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string? Headers { get; set; }
    public DateTime FirstFailureTime { get; set; }
    public DateTime LastFailureTime { get; set; }
    public int RetryCount { get; set; }
    public DateTime NextRetryAt { get; set; }
    public RetryRecordStatus Status { get; set; }
    public string? LastErrorMessage { get; set; }
    public string? LastErrorType { get; set; }
    public string? CorrelationId { get; set; }
}

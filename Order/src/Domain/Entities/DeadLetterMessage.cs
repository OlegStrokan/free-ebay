namespace Domain.Entities;

// represents a message that failed to process after maximum retries
// purpose: Katka can check proc se to kurva stalo
public class DeadLetterMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public DateTime OccurredOn { get; private set; }
    public string FailureReason { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime MovedToDeadLetterAt { get; private set; }
    
    public int DeadLetterRetryCount { get; private set; }
    public DateTime? LastRetryAttempt { get; private set; }
    
    public bool IsResolved { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }
    
    private DeadLetterMessage() {}

    public DeadLetterMessage(
        Guid id,
        string type,
        string content,
        DateTime occurredOn,
        string failureReason,
        int retryCount)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredOn = occurredOn;
        FailureReason = failureReason;
        RetryCount = retryCount;
        MovedToDeadLetterAt = DateTime.UtcNow;
        DeadLetterRetryCount = 0;
        IsResolved = false;
    }

    public static DeadLetterMessage Create(
        Guid messageId,
        string type,
        string content,
        DateTime occurredOn,
        string failureReason,
        int retryCount)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("MessageId cannot be empty", nameof(messageId));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type is required", nameof(type));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required", nameof(content));

        if (string.IsNullOrWhiteSpace(failureReason))
            throw new ArgumentException("FailureReason is required", nameof(failureReason));

        return new DeadLetterMessage(messageId, type, content, occurredOn, failureReason, retryCount);
    }

    public void IncrementRetryCount()
    {
        DeadLetterRetryCount++;
        LastRetryAttempt = DateTime.UtcNow;
    }

    public void MarkAsResolved(string resolutionNotes)
    {
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
        ResolutionNotes = resolutionNotes;
    }
}
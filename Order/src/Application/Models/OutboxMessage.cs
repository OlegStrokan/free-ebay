namespace Application.Models;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime OccurredOnUtc { get; private set; }
    
    public DateTime? ProcessedOnUtc { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? LastRetryAtUtc { get; set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }
    
    public OutboxMessage(Guid id, string type, string content, DateTime occurredOnUtc)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
        RetryCount = 0;
    }

    public void MarkAsProcessed(DateTime processedAt)
    {
        ProcessedOnUtc = processedAt;
        Error = null;

    }

    public void UpdateFailure(string error, DateTime retryAt)
    {
        Error = error;
        RetryCount++;
        LastRetryAtUtc = retryAt;
    }
}
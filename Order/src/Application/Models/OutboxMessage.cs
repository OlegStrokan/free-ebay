namespace Application.Models;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime OccurredOnUtc { get; private set; }
    
    public DateTime? ProcessedOnUtc { get; private set; }
    public int RetryCount { get; set; }
    public DateTime? LastRetryAtUtc { get; set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }
    
    public OutboxMessage(Guid id, string type, string content, DateTime occurredOnUtc)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
    }

    public void MarkAsProcessed(DateTime processedAt)
    {
        ProcessedOnUtc = processedAt;
    }

    public void LogError(string error)
    {
        Error = error;
    }
}
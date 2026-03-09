namespace Application.Models;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime OccurredOn { get; private set; }
    public string AggregateId { get; private set; } = string.Empty;
    public DateTime? ProcessedOn { get; private set; }
    public int RetryCount { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { } // EF Core

    public OutboxMessage(Guid id, string type, string content, DateTime occurredOn, string aggregateId)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredOn = occurredOn;
        AggregateId = aggregateId;
    }

    public void MarkAsProcessed() => ProcessedOn = DateTime.UtcNow;

    public void IncrementRetry(string error)
    {
        RetryCount++;
        Error = error;
    }

    public void MarkFailed(string error) => Error = error;
}

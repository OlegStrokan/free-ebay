namespace Infrastructure.Persistence.Entities;

public sealed class OutboxMessageEntity
{
    public Guid OutboxMessageId { get; set; }

    public string Topic { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public int RetryCount { get; set; }

    public string LastError { get; set; } = string.Empty;
}

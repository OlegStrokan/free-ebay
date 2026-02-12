namespace Domain.Entities;

public class ProcessedEvent
{
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = null!;
    public DateTime ProcessedAt { get; private set; }
    public string ProcessedBy { get; private set; } = null!;
    
    private ProcessedEvent() {}

    private ProcessedEvent(
        Guid eventId,
        string eventType,
        string processedBy)
    {
        EventId = eventId;
        EventType = eventType;
        ProcessedBy = processedBy;
        ProcessedAt = DateTime.UtcNow;
    }

    public static ProcessedEvent Create(
        Guid eventId,
        string eventType,
        string processedBy)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("EventId cannot be empty", nameof(eventId));

        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType is required", nameof(eventType));

        if (string.IsNullOrWhiteSpace(processedBy))
            throw new ArgumentException("ProcessedBy is required", nameof(processedBy));

        return new ProcessedEvent(eventId, eventType, processedBy);
    }
    
}
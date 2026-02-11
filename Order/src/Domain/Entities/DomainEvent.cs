namespace Domain.Entities;

public class DomainEvent
{
    public Guid EventId { get; private set; }
    public string AggregateId { get; private set; } = null!;
    public string AggregateType { get; private set; } = null!;
    public string EventType { get; private set; } = null!;
    public string EventData { get; private set; } = null!;
    public int Version { get; private set; }
    public DateTime OccuredOn { get; private set; }

    private DomainEvent()
    {
        
    }

    private DomainEvent(
        Guid eventId,
        string aggregateId,
        string aggregateType,
        string eventType,
        string eventData,
        int version,
        DateTime occuredOn)
    {
        EventId = eventId;
        AggregateId = aggregateId;
        AggregateType = aggregateType;
        EventType = eventType;
        EventData = eventData;
        Version = version;
        OccuredOn = occuredOn;
    }

    public static DomainEvent Create(
        string aggregateId,
        string aggregateType,
        string eventType,
        string eventData,
        int version)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
            throw new ArgumentException("AggregateId is required", nameof(aggregateId));

        if (string.IsNullOrWhiteSpace(aggregateType))
            throw new ArgumentException("AggregateType is required", nameof(aggregateType));

        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType is required", nameof(eventType));

        return new DomainEvent(
            Guid.NewGuid(),
            aggregateId,
            aggregateType,
            eventType,
            eventData,
            version,
            DateTime.UtcNow);
    }
}
using Domain.Common;

namespace Domain.Entities;

public class DomainEvent : IDomainEvent
{
    public Guid EventId { get; private set; }
    public string AggregateId { get; private set; } = null!;
    public string AggregateType { get; private set; } = null!;
    public string EventType { get; private set; } = null!;
    public string EventData { get; private set; } = null!;
    public int Version { get; private set; }
    public DateTime OccurredOn { get; private set; }
    
    private DomainEvent(
        Guid eventId,
        string aggregateId,
        string aggregateType,
        string eventType,
        string eventData,
        int version,
        DateTime occurredOn)
    {
        EventId = eventId;
        AggregateId = aggregateId;
        AggregateType = aggregateType;
        EventType = eventType;
        EventData = eventData;
        Version = version;
        OccurredOn = occurredOn;
    }

    public static DomainEvent Create(
        string aggregateId,
        string aggregateType,
        string eventType,
        string eventData,
        int version,
        Guid eventId)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
            throw new ArgumentException("AggregateId is required", nameof(aggregateId));

        if (string.IsNullOrWhiteSpace(aggregateType))
            throw new ArgumentException("AggregateType is required", nameof(aggregateType));

        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType is required", nameof(eventType));

        return new DomainEvent(
            eventId,
            aggregateId,
            aggregateType,
            eventType,
            eventData,
            version,
            DateTime.UtcNow);
    }
}
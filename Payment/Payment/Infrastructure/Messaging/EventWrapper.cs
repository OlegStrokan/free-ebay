namespace Infrastructure.Messaging;

internal sealed class EventWrapper
{
    public Guid EventId { get; init; }

    public string EventType { get; init; } = string.Empty;

    public string Payload { get; init; } = string.Empty;

    public DateTime OccurredOn { get; init; }
}
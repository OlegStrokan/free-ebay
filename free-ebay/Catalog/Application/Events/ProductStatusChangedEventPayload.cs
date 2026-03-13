namespace Application.Events;

// Deserialized from EventWrapper.Payload when EventType == "ProductStatusChangedEvent"
public sealed record ProductStatusChangedEventPayload
{
    public required ProductIdPayload ProductId { get; init; }
    public required string PreviousStatus { get; init; }
    public required string NewStatus { get; init; }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
}

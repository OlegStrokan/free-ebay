namespace Application.Events;

// Deserialized from EventWrapper.Payload when EventType == "ProductDeletedEvent"
public sealed record ProductDeletedEventPayload
{
    public required ProductIdPayload ProductId { get; init; }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
}

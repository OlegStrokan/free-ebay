namespace Application.Events;

// Deserialized from EventWrapper.Payload when EventType == "ProductStockUpdatedEvent"
public sealed record ProductStockUpdatedEventPayload
{
    public required ProductIdPayload ProductId { get; init; }
    public required int PreviousQuantity { get; init; }
    public required int NewQuantity { get; init; }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
}

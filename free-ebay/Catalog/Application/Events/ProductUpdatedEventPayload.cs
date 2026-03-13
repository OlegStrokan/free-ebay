namespace Application.Events;

// Deserialized from EventWrapper.Payload when EventType == "ProductUpdatedEvent"
// Note: stock and status are NOT present — those are separate domain events (ProductStockUpdatedEvent,
// ProductStatusChangedEvent). Add dedicated consumers for those when real-time stock/status sync is needed.
public sealed record ProductUpdatedEventPayload
{
    public required ProductIdPayload ProductId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required CategoryIdPayload CategoryId { get; init; }
    public required MoneyPayload Price { get; init; }
    public required List<ProductAttributePayload> Attributes  { get; init; }
    public required List<string> ImageUrls { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
}

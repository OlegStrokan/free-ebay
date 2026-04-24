namespace Application.Events;

// Deserialized from EventWrapper.Payload when EventType == "ProductCreatedEvent"
public sealed record ProductCreatedEventPayload
{
    public required ProductIdPayload ProductId { get; init; }
    public required SellerIdPayload SellerId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required CategoryIdPayload CategoryId { get; init; }
    public required MoneyPayload Price { get; init; }
    public required int InitialStock { get; init; }
    public required List<ProductAttributePayload> Attributes { get; init; }
    public required List<string> ImageUrls { get; init; }
    public required DateTime CreatedAt { get; init; }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
}

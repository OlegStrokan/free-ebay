namespace Application.Events;

// Matches the JSON produced by Inventory's BuildInventoryReservationPayload
// (used for InventoryConfirmed, InventoryReleased, InventoryExpired)
public sealed record InventoryItemPayload
{
    public required string ProductId { get; init; }
    public required int Quantity { get; init; }
}

public sealed record InventoryReservationEventPayload
{
    public required string ReservationId { get; init; }
    public required string OrderId { get; init; }
    public string? Status { get; init; }
    public required IReadOnlyList<InventoryItemPayload> Items { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}

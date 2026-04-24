namespace Gateway.Api.Contracts.Inventory;

public sealed record ReserveInventoryRequest(string OrderId, IReadOnlyList<InventoryItemDto> Items);
public sealed record InventoryItemDto(string ProductId, int Quantity);
public sealed record ReserveInventoryResponse(bool Success, string ReservationId, string? ErrorMessage);

public sealed record ReleaseInventoryRequest(string ReservationId);
public sealed record ReleaseInventoryResponse(bool Success, string? ErrorMessage);

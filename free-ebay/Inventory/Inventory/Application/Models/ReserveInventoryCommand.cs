namespace Application.Models;

public sealed record ReserveInventoryCommand(
    Guid OrderId,
    IReadOnlyCollection<ReserveInventoryItemInput> Items);

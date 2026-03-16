namespace Application.Models;

public sealed record ReserveInventoryItemInput(Guid ProductId, int Quantity);

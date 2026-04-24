namespace Infrastructure.Persistence.Entities;

public sealed class InventoryMovementEntity
{
    public Guid MovementId { get; set; }

    public Guid ProductId { get; set; }

    public string MovementType { get; set; } = string.Empty;

    public int QuantityDelta { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}

namespace Infrastructure.Persistence.Entities;

public sealed class ProductStockEntity
{
    public Guid ProductId { get; set; }

    public int AvailableQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

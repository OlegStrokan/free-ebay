namespace Infrastructure.Persistence.Entities;

public sealed class InventoryReservationEntity
{
    public Guid ReservationId { get; set; }

    public Guid OrderId { get; set; }

    public string Status { get; set; } = ReservationStatus.Active;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public List<InventoryReservationItemEntity> Items { get; set; } = [];
}

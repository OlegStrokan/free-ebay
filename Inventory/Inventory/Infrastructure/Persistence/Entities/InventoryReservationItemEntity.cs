namespace Infrastructure.Persistence.Entities;

public sealed class InventoryReservationItemEntity
{
    public Guid ReservationItemId { get; set; }

    public Guid ReservationId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public InventoryReservationEntity Reservation { get; set; } = null!;
}

namespace Application.DTOs;

public record ReturnShipmentDeliveredEventDto
{
    public Guid OrderId { get; init; }
    public string ShipmentId { get; init; } = string.Empty;
    public string? TrackingNumber { get; init; }
    public DateTime DeliveredAt { get; init; }
}
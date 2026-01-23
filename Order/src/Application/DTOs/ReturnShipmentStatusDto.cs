namespace Application.DTOs;

public record ReturnShipmentStatusDto(
    string ShipmentId,
    string Status,
    DateTime? ReceivedAt,
    string TrackingNumber
    );
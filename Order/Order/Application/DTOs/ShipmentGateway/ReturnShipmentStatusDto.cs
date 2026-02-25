namespace Application.DTOs.ShipmentGateway;

public record ReturnShipmentStatusDto(
        string ReturnTrackingNumber,
        string Status,
        DateTime? PickedUpAt,
        DateTime? DeliveredAt
    );
namespace Application.DTOs.ShipmentGateway;

public record ReturnShipmentResultDto(
    string ReturnShipmentId,
    string ReturnTrackingNumber,
    DateTime ExpectedPickupDate
    );
namespace Application.DTOs.ShipmentGateway;

public record ShipmentStatusDto(
    string TrackingNumber,
    string Status,
    DateTime? EstimatedDeliveryDate,
    DateTime? ActualDeliveryDate,
    string? CurrentLocation);
namespace Application.DTOs;

public record OrderSnapshotState(
    Guid Id, Guid CustomerId, string Status, decimal TotalAmount,
    string Currency, string Street, string City, string Country, string PostalCode,
    string? TrackingId, string? PaymentId, int Version, DateTime CreatedAt);

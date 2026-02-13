namespace Infrastructure.ReadModels;

public class OrderReadModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string? TrackingId { get; set; }
    public string? PaymentId { get; set; }
    public string Status { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = null!;

    public string DeliveryStreet { get; set; } = null!;
    public string DeliveryCity { get; set; } = null!;
    public string DeliveryState { get; set; } = null!;
    public string DeliveryCountry { get; set; } = null!;
    public string DeliveryPostalCode { get; set; } = null!;

    public string ItemsJson { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public int Version { get; set; }
    
    public DateTime LastSyncedAt { get; set; }
}
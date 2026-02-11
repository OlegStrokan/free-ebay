namespace Infrastructure.ReadModels;

public class OrderReadModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? TrackingId { get; set; }
    public Guid? PaymentId { get; set; }
    public string Status { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = null!;

    public string DeliveryStreet { get; set; } = null!;
    public string DeliveryCity { get; set; } = null!;
    public string DeliveryState { get; set; } = null!;
    public string DeliveryCountry { get; set; } = null!;
    public string DeliveryPostalCode { get; set; } = null!;

    public string ItemsJson { get; set; } = null!;
    
    public DateTime CreateAt { get; set; }
    public DateTime? UpdateAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public int Version { get; set; }
    
    public DateTime LastSyncedAt { get; set; }
}
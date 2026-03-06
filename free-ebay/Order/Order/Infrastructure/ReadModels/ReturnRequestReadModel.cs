namespace Infrastructure.ReadModels;

public class ReturnRequestReadModel
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public decimal RefundAmount { get; set; }
    public string Currency { get; set; } = null!;

    public string ItemsToReturnJson { get; set; } = null!;
    
    public DateTime RequestedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // for optimistic concurrency in read model updates
    public int Version { get; set; }
    
    public DateTime LastSyncedAt { get; set; }
}
namespace Infrastructure.ReadModels;

public class RecurringOrderReadModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public string Frequency { get; set; } = null!;
    public string Status { get; set; } = null!;

    public DateTime NextRunAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public int TotalExecutions { get; set; }
    public int? MaxExecutions { get; set; }

    public string DeliveryStreet { get; set; } = null!;
    public string DeliveryCity { get; set; } = null!;
    public string DeliveryCountry { get; set; } = null!;
    public string DeliveryPostalCode { get; set; } = null!;

    public string ItemsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int Version { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
}

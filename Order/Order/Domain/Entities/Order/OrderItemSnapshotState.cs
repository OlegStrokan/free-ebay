namespace Domain.Entities.Order;

public record OrderItemSnapshotState(
    long ItemId,
    Guid? OrderId,
    Guid ProductId,
    int Quantity,
    decimal Price,
    string Currency);
    
    public record OrderSnapshotState(
        Guid Id,
        Guid CustomerId,
        string Status,
        decimal TotalAmount,
        string Currency,
        string Street,
        string City,
        string Country,
        string PostalCode,
        string? TrackingId,
        string? PaymentId,
        int Version,
        DateTime CreatedAt,
        DateTime? CompletedAt,
        DateTime? UpdatedAt,
        List<string> FailedMessages,
        List<OrderItemSnapshotState> Items
        );
namespace Domain.Entities.Subscription;

public record RecurringOrderSnapshotState(
    Guid Id,
    Guid CustomerId,
    string PaymentMethod,
    string Frequency,
    string Status,
    DateTime NextRunAt,
    DateTime? LastRunAt,
    int TotalExecutions,
    int? MaxExecutions,
    string Street,
    string City,
    string Country,
    string PostalCode,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int Version,
    List<RecurringOrderItemSnapshotState> Items);

public record RecurringOrderItemSnapshotState(
    Guid   ProductId,
    int    Quantity,
    decimal Price,
    string Currency);

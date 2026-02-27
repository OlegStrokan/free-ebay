namespace Domain.Entities;

// synchronous projection: OrderId => ReturnRequestId
public class ReturnRequestLookup
{
    public Guid OrderId { get; private set; }
    public Guid ReturnRequestId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ReturnRequestLookup() { }

    public static ReturnRequestLookup Create(Guid orderId, Guid returnRequestId) =>
        new()
        {
            OrderId = orderId,
            ReturnRequestId = returnRequestId,
            CreatedAt = DateTime.UtcNow
        };
}

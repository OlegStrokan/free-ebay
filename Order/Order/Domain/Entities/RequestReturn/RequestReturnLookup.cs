namespace Domain.Entities.RequestReturn;

// synchronous projection: OrderId => ReturnRequestId
public class RequestReturnLookup
{
    public Guid OrderId { get; private set; }
    public Guid ReturnRequestId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private RequestReturnLookup() { }

    public static RequestReturnLookup Create(Guid orderId, Guid returnRequestId) =>
        new()
        {
            OrderId = orderId,
            ReturnRequestId = returnRequestId,
            CreatedAt = DateTime.UtcNow
        };
}

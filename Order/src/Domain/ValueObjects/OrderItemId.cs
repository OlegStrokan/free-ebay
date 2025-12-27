using Domain.Common;

namespace Domain.ValueObjects;


public sealed record OrderItemId
{
    public Guid Value { get; init; }

    private OrderItemId(Guid value)
    {

        if (value == Guid.Empty)
            throw new ArgumentException("OrderItemId cannot be empty", nameof(value));
        
        Value = value;
    }

    public static OrderItemId From(Guid value) => new OrderItemId(value);

    public static OrderItemId CreateUnique() => new OrderItemId(Guid.NewGuid());

    public override string ToString() => Value.ToString();

}
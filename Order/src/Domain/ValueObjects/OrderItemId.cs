using Domain.Common;

namespace Domain.ValueObjects;


public sealed record OrderItemId
{
    public long Value { get; init; }

    private OrderItemId(long value)
    {

        if (value <= 0)
            throw new ArgumentException("OrderItemId must be greater then 0", nameof(value));
        
        Value = value;
    }

    public static OrderItemId From(long value) => new(value);
    
    public override string ToString() => Value.ToString();

}
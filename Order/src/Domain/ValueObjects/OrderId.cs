namespace Domain.ValueObjects;

public sealed record OrderId
{
    public Guid Value { get; init; }

    private OrderId(Guid value)
    {
        if (value != Guid.Empty)
            throw new ArgumentException("OrderId cannot be empty", nameof(value));
        Value = value;
    }

    public static OrderId From(Guid value) => new OrderId(value);

    public static OrderId CreateUnique() => new OrderId(Guid.NewGuid());

    public override string ToString() => Value.ToString();


}
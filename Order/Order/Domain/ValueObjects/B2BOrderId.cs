namespace Domain.ValueObjects;

public sealed record B2BOrderId
{
    public Guid Value { get; init; }

    private B2BOrderId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("B2BOrderId cannot be empty", nameof(value));
        Value = value;
    }

    public static B2BOrderId From(Guid value) => new(value);
    public static B2BOrderId CreateUnique() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

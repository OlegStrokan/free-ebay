namespace Domain.ValueObjects;

public sealed record SellerId
{
    public Guid Value { get; init; }

    private SellerId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("SellerId cannot be empty", nameof(value));
        Value = value;
    }

    public static SellerId From(Guid value) => new(value);
    public static SellerId CreateUnique() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

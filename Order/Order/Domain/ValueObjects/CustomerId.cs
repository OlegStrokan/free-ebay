namespace Domain.ValueObjects;

public sealed record CustomerId
{
    public Guid Value { get; init; }

    private CustomerId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty", nameof(value));

        Value = value;
    }

    public static CustomerId From(Guid value) => new CustomerId(value);

    public static CustomerId CreateUnique() => new CustomerId(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
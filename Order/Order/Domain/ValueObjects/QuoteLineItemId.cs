namespace Domain.ValueObjects;

public sealed record QuoteLineItemId
{
    public Guid Value { get; init; }

    private QuoteLineItemId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("QuoteLineItemId cannot be empty", nameof(value));
        Value = value;
    }

    public static QuoteLineItemId From(Guid value) => new(value);
    public static QuoteLineItemId CreateUnique() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

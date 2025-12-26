namespace Domain.ValueObjects;

public sealed record ProductId(string Value) : ValueObject
{
    public static ProductId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ID cannot be empty", nameof(value));

        return new ProductId(value);
    }
}
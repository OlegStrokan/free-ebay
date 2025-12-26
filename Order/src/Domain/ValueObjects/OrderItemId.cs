namespace Domain.ValueObjects;

public sealed record OrderItemId(string Value) : ValueObject
{
    public static OrderItemId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ID cannot be empty", nameof(value));

        return new OrderItemId(value);
    }
}
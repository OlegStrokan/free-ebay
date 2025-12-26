namespace Domain.ValueObjects;

public sealed record OrderId(string Value) : ValueObject
{
    
    public static OrderId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ID cannot be empty", nameof(value));
            
        return new OrderId(value);
    }
}
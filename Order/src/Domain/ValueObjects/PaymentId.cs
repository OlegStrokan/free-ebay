namespace Domain.ValueObjects;

public sealed record PaymentId
{
    public Guid Value { get; init; }

    private PaymentId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PaymentId cannot be empty", nameof(value));
        Value = value;
    }

    public static PaymentId From(Guid value) => new PaymentId(value);
    
    public static PaymentId CreateUnique() => new PaymentId(Guid.NewGuid());
    
    public override string ToString() => Value.ToString();
}
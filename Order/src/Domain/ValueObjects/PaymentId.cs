namespace Domain.ValueObjects;


public sealed record PaymentId
{
    public string Value { get; init; }


    private PaymentId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("PaymentId cannot be empty", nameof(value));
        Value = value;
    }

    public static PaymentId From(string value) => new PaymentId(value);
    public static implicit operator string(PaymentId p) => p.Value;
}
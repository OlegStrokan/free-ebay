using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed record PaymentId
{
    public string Value { get; init; }

    private PaymentId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("PaymentId cannot be empty");
        }

        Value = value.Trim();
    }

    public static PaymentId From(string value) => new(value);

    public static PaymentId CreateUnique() => new(Guid.NewGuid().ToString("N"));

    public static implicit operator string(PaymentId valueObject) => valueObject.Value;

    public override string ToString() => Value;
}
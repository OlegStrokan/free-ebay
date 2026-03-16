using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed record ProviderPaymentIntentId
{
    public string Value { get; init; }

    private ProviderPaymentIntentId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("Provider payment intent id cannot be empty");
        }

        Value = value.Trim();
    }

    public static ProviderPaymentIntentId From(string value) => new(value);

    public static implicit operator string(ProviderPaymentIntentId valueObject) => valueObject.Value;

    public override string ToString() => Value;
}
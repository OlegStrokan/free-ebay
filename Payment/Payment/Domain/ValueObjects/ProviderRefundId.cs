using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed record ProviderRefundId
{
    public string Value { get; init; }

    private ProviderRefundId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("Provider refund id cannot be empty");
        }

        Value = value.Trim();
    }

    public static ProviderRefundId From(string value) => new(value);

    public static implicit operator string(ProviderRefundId valueObject) => valueObject.Value;

    public override string ToString() => Value;
}
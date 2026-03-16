using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed record RefundId
{
    public string Value { get; init; }

    private RefundId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("RefundId cannot be empty");
        }

        Value = value.Trim();
    }

    public static RefundId From(string value) => new(value);

    public static RefundId CreateUnique() => new(Guid.NewGuid().ToString("N"));

    public static implicit operator string(RefundId valueObject) => valueObject.Value;

    public override string ToString() => Value;
}
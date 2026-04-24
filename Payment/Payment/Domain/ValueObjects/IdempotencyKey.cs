using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed record IdempotencyKey
{
    private const int MaxLength = 128;

    public string Value { get; init; }

    private IdempotencyKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("Idempotency key cannot be empty");
        }

        var normalized = value.Trim();
        if (normalized.Length > MaxLength)
        {
            throw new InvalidValueException($"Idempotency key cannot exceed {MaxLength} characters");
        }

        Value = normalized;
    }

    public static IdempotencyKey From(string value) => new(value);

    public static implicit operator string(IdempotencyKey valueObject) => valueObject.Value;

    public override string ToString() => Value;
}
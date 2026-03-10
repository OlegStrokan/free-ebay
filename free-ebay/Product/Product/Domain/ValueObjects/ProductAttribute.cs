using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed record ProductAttribute(string Key, string Value)
{
    public string Key { get; init; } =
        !string.IsNullOrWhiteSpace(Key)
            ? Key.Trim().ToLowerInvariant()
            : throw new InvalidValueException("Attribute key cannot be empty");

    public string Value { get; init; } =
        !string.IsNullOrWhiteSpace(Value)
            ? Value.Trim()
            : throw new InvalidValueException("Attribute value cannot be empty");
}
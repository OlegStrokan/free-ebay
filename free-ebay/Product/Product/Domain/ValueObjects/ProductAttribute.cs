namespace Domain.ValueObjects;

public sealed record ProductAttribute(string Key, string Value)
{
    public string Key { get; init; } =
        !string.IsNullOrWhiteSpace(Key)
            ? Key.Trim().ToLowerInvariant()
            : throw new ArgumentException("Attribute key cannot be empty", nameof(Key));

    public string Value { get; init; } =
        !string.IsNullOrWhiteSpace(Value)
            ? Value.Trim()
            : throw new ArgumentException("Attribute value cannot be empty", nameof(Value));
}
namespace Application.Events;

// These must match the JSON structure produced by Product's domain value objects.

public sealed record ProductIdPayload
{
    public Guid Value { get; init; }
}

public sealed record SellerIdPayload
{
    public Guid Value { get; init; }
}

public sealed record CategoryIdPayload
{
    public Guid Value { get; init; }
}

public sealed record MoneyPayload
{
    public decimal Amount   { get; init; }
    public string  Currency { get; init; } = string.Empty;
}

public sealed record ProductAttributePayload
{
    public string Key   { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

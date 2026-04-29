using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed record CatalogItemId
{
    public Guid Value { get; init; }

    private CatalogItemId(Guid value)
    {
        if (value == Guid.Empty)
            throw new InvalidValueException("CatalogItemId cannot be empty");

        Value = value;
    }

    public static CatalogItemId From(Guid value) => new(value);

    public static CatalogItemId CreateUnique() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed record ListingId
{
    public Guid Value { get; init; }

    private ListingId(Guid value)
    {
        if (value == Guid.Empty)
            throw new InvalidValueException("ListingId cannot be empty");

        Value = value;
    }

    public static ListingId From(Guid value) => new(value);

    public static ListingId CreateUnique() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
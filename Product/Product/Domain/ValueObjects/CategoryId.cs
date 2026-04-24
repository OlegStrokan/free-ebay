using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed record CategoryId
{
    public Guid Value { get; init; }

    private CategoryId(Guid value)
    {
        if (value == Guid.Empty)
            throw new InvalidValueException("CategoryId cannot be empty");
        Value = value;
    }

    public static CategoryId From(Guid value) => new(value);
    public static CategoryId CreateUnique() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

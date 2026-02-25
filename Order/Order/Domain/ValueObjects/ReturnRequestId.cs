namespace Domain.ValueObjects;

public sealed record ReturnRequestId
{
    public Guid Value { get; init; }

    private ReturnRequestId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("ReturnRequestId cannot be empty", nameof(value));
        Value = value;
    }

    public static ReturnRequestId From(Guid value) => new ReturnRequestId(value);

    public static ReturnRequestId CreateUnique() => new ReturnRequestId(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
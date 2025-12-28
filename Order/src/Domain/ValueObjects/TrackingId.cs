namespace Domain.ValueObjects;

public sealed record TrackingId
{
    public Guid Value { get; init; }

    private TrackingId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("TrackingId cannot be empty", nameof(value));
        Value = value;
    }

    public static TrackingId From(Guid value) => new TrackingId(value);

    public static TrackingId CreateUnique() => new TrackingId(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
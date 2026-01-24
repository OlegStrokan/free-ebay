namespace Domain.ValueObjects;


public sealed record TrackingId
{
    public string Value { get; init; }


    private TrackingId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("TrackingId cannot be empty", nameof(value));
        Value = value;
    }

    public static TrackingId From(string value) => new TrackingId(value);
    public static implicit operator string(TrackingId p) => p.Value;
}
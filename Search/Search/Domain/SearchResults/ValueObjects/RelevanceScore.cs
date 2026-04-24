namespace Domain.SearchResults.ValueObjects;

public sealed record RelevanceScore
{
    public double Value { get; }

    public RelevanceScore(double value)
    {
        if (value < 0)
            throw new ArgumentException("Score must be non-negative");

        Value = value;
    }

    public static implicit operator double(RelevanceScore score) => score.Value;
}
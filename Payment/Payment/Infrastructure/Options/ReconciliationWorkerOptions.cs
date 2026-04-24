namespace Infrastructure.Options;

public sealed class ReconciliationWorkerOptions
{
    public const string SectionName = "ReconciliationWorker";

    public bool Enabled { get; init; } = true;

    public int IntervalSeconds { get; init; } = 60;

    public int OlderThanMinutes { get; init; } = 15;

    public int BatchSize { get; init; } = 100;
}
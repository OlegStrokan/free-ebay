namespace Infrastructure.Messaging;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int BatchSize { get; init; } = 20;

    public int PollIntervalMs { get; init; } = 2000;

    public int MaxRetries { get; init; } = 5;
}

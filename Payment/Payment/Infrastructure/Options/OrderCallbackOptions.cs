namespace Infrastructure.Options;

public sealed class OrderCallbackOptions
{
    public const string SectionName = "OrderCallback";

    public string EndpointUrl { get; init; } = string.Empty;

    public string SharedSecret { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 10;

    public int PollIntervalSeconds { get; init; } = 5;

    public int BatchSize { get; init; } = 100;

    public int MaxAttempts { get; init; } = 8;

    public int BaseRetryDelaySeconds { get; init; } = 5;

    public int MaxRetryDelaySeconds { get; init; } = 300;
}
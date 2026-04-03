namespace Infrastructure.BackgroundServices;

public sealed class ReservationExpiryOptions
{
    public const string SectionName = "ReservationExpiry";

    public int BatchSize { get; init; } = 50;

    public int PollIntervalMs { get; init; } = 60000;

    public int ReservationTtlMinutes { get; init; } = 30;

    public TimeSpan ReservationTtl => TimeSpan.FromMinutes(ReservationTtlMinutes);
}
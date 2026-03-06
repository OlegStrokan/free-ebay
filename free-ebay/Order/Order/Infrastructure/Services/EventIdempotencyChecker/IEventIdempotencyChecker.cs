namespace Infrastructure.Services.EventIdempotencyChecker;

public interface IEventIdempotencyChecker
{
    Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken ct = default);
    Task MarkAsProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default);
}
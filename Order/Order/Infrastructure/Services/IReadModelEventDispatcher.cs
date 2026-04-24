namespace Infrastructure.Services;

public interface IReadModelEventDispatcher
{
    Task<bool> DispatchAsync(
        string eventType,
        string aggregateId,
        string eventData,
        CancellationToken ct);
}

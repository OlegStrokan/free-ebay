namespace Application.Interfaces;

public sealed record IdempotencyRecord(
    string Key,
    Guid ResultId,
    DateTime CreatedAt);

public interface IIdempotencyRepository
{
    Task<IdempotencyRecord?> GetByKeyAsync(string idempotencyKey, CancellationToken ct);
    Task SaveAsync(string idempotencyKey, Guid orderId, DateTime createdAt, CancellationToken ct);
    
}
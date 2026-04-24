namespace Application.Interfaces;

public sealed record IdempotencyRecord
{
    public string Key { get; private set; } = string.Empty;
    public Guid ResultId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private IdempotencyRecord() {}

    public IdempotencyRecord(string key, Guid resultId, DateTime createdAt)
    {
        Key = key;
        ResultId = resultId;
        CreatedAt = createdAt;
    }
    
}

public interface IIdempotencyRepository
{
    Task<IdempotencyRecord?> GetByKeyAsync(string idempotencyKey, CancellationToken ct);
    Task SaveAsync(string idempotencyKey, Guid orderId, DateTime createdAt, CancellationToken ct);
    
}
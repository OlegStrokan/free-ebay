using Application.Interfaces;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of idempotency repository using database table
/// </summary>
public sealed class IdempotencyRepository(AppDbContext dbContext) : IIdempotencyRepository
{
    public async Task<IdempotencyRecord?> GetByKeyAsync(string idempotencyKey, CancellationToken ct)
    {
        var record = await dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(x => x.Key == idempotencyKey, ct);

        if (record == null)
            return null;

        return new IdempotencyRecord(record.Key, record.ResultId, record.CreatedAt);
    }

    public async Task SaveAsync(
        string idempotencyKey,
        Guid orderId,
        DateTime createdAt,
        CancellationToken ct)
    {
        var record = new IdempotencyRecordEntity
        {
            Key = idempotencyKey,
            ResultId = orderId,
            CreatedAt = createdAt
        };

        await dbContext.IdempotencyRecords.AddAsync(record, ct);
    }
}

// Database entity
public class IdempotencyRecordEntity
{
    public string Key { get; set; } = string.Empty;  // Primary key
    public Guid ResultId { get; set; }
    public DateTime CreatedAt { get; set; }
}

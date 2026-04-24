using Application.Interfaces;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

// used by handlers and persistence services
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
        // why we dont have for another events? because idempotency entity use eventId which is not generated
        var existingEntry = dbContext.ChangeTracker
            .Entries<IdempotencyRecord>()
            .FirstOrDefault(e => e.Entity.Key == idempotencyKey);
        if (existingEntry is not null)
            existingEntry.State = EntityState.Detached;

        var record = new IdempotencyRecord(idempotencyKey, orderId, createdAt);

        await dbContext.IdempotencyRecords.AddAsync(record, ct);
    }
}


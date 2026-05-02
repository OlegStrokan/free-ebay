using System.Text.Json;
using Application.Interfaces;
using Infrastructure.Persistence.DbContext;
using Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class RecurringOrderReadRepository(ReadDbContext dbContext) : IRecurringOrderReadRepository
{
    public async Task<RecurringOrderDetail?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var model = await dbContext.RecurringOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        return model is null ? null : MapToDetail(model);
    }

    public async Task<List<RecurringOrderSummary>> GetByCustomerIdAsync(
        Guid customerId, CancellationToken ct = default)
    {
        var models = await dbContext.RecurringOrderReadModels
            .AsNoTracking()
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return models.Select(MapToSummary).ToList();
    }

    public async Task<List<RecurringOrderSummary>> GetDueAsync(
        DateTime asOf, int limit, CancellationToken ct = default)
    {
        var models = await dbContext.RecurringOrderReadModels
            .AsNoTracking()
            .Where(r => r.Status == "Active" && r.NextRunAt <= asOf)
            .OrderBy(r => r.NextRunAt)       // oldest-due first
            .Take(limit)
            .ToListAsync(ct);

        return models.Select(MapToSummary).ToList();
    }

    public async Task<List<RecurringOrderSummary>> GetAllAsync(
        int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var models = await dbContext.RecurringOrderReadModels
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return models.Select(MapToSummary).ToList();
    }
    
    private static RecurringOrderDetail MapToDetail(RecurringOrderReadModel m)
    {
        var items = JsonSerializer.Deserialize<List<RecurringOrderItemResponse>>(m.ItemsJson)
                    ?? new List<RecurringOrderItemResponse>();

        return new RecurringOrderDetail(
            Id:              m.Id,
            CustomerId:      m.CustomerId,
            PaymentMethod:   m.PaymentMethod,
            Frequency:       m.Frequency,
            Status:          m.Status,
            NextRunAt:       m.NextRunAt,
            LastRunAt:       m.LastRunAt,
            TotalExecutions: m.TotalExecutions,
            MaxExecutions:   m.MaxExecutions,
            DeliveryAddress: new AddressResponse(
                m.DeliveryStreet, m.DeliveryCity, m.DeliveryCountry, m.DeliveryPostalCode),
            Items:     items,
            CreatedAt: m.CreatedAt,
            UpdatedAt: m.UpdatedAt,
            Version:   m.Version);
    }

    public async Task<List<RecurringOrderSummary>> ClaimDueAsync(
        DateTime asOf, int limit, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var staleThreshold = now.AddMinutes(-5);

        // replicas never schedule the same recurring order twice
        var claimedIds = await dbContext.Database
            .SqlQueryRaw<Guid>(
                """
                UPDATE "RecurringOrderReadModels"
                SET "ClaimedAtUtc" = {0}
                WHERE "Id" IN (
                    SELECT "Id" FROM "RecurringOrderReadModels"
                    WHERE "Status" = 'Active' AND "NextRunAt" <= {1}
                      AND ("ClaimedAtUtc" IS NULL OR "ClaimedAtUtc" < {2})
                    ORDER BY "NextRunAt"
                    LIMIT {3}
                    FOR UPDATE SKIP LOCKED
                )
                RETURNING "Id"
                """,
                now, asOf, staleThreshold, limit)
            .ToListAsync(ct);

        if (claimedIds.Count == 0)
            return [];

        var models = await dbContext.RecurringOrderReadModels
            .AsNoTracking()
            .Where(r => claimedIds.Contains(r.Id))
            .OrderBy(r => r.NextRunAt)
            .ToListAsync(ct);

        return models.Select(MapToSummary).ToList();
    }

    private static RecurringOrderSummary MapToSummary(RecurringOrderReadModel m) =>
        new(m.Id, m.CustomerId, m.Frequency, m.Status, m.NextRunAt, m.TotalExecutions, m.CreatedAt);
}

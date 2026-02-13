using System.Text.Json;
using Application.Interfaces;
using Infrastructure.Persistence.DbContext;
using Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

// read-only repository, uses denormalized OrderReadModel instead of replaying events
public class OrderReadRepository(AppDbContext dbContext) : IOrderReadRepository
{
    public async Task<OrderResponse?> GetByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var readModel = await dbContext.OrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (readModel == null)
            return null;

        return MapToResponse(readModel);
    }


    public async Task<OrderResponse?> GetByTrackingIdAsync(string trackingId, CancellationToken ct = default)
    {
        var readModel = await dbContext.OrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.TrackingId == trackingId, ct);

        if (readModel == null)
            return null;

        return MapToResponse(readModel);
    }

    public async Task<List<OrderSummaryResponse>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        var readModels = await dbContext.OrderReadModels
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return readModels.Select(MapToSummary).ToList();
    }

    public async Task<List<OrderSummaryResponse>> GetOrderAsync(
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var readModels = await dbContext.OrderReadModels
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return readModels.Select(MapToSummary).ToList();
    }

    private static OrderResponse MapToResponse(OrderReadModel model)
    {
        var items = JsonSerializer.Deserialize<List<OrderItemResponse>>(model.ItemsJson)
                    ?? new List<OrderItemResponse>();

        return new OrderResponse(
            model.Id,
            model.CustomerId,
            model.TrackingId,
            model.Status,
            model.TotalAmount,
            model.Currency,
            new AddressResponse(
                model.DeliveryStreet,
                model.DeliveryCity,
                model.DeliveryState,
                model.DeliveryCountry,
                model.DeliveryPostalCode
            ),
            items,
            model.CreatedAt,
            model.UpdatedAt,
            model.Version);
    }
  
    private static OrderSummaryResponse MapToSummary(OrderReadModel model)
    {
        return new OrderSummaryResponse(
            model.Id,
            model.TrackingId,
            model.Status,
            model.TotalAmount,
            model.Currency,
            model.CreatedAt);
    }
}
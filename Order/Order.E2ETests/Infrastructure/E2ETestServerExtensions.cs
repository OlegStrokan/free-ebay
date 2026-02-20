using Application.Interfaces;
using Application.Sagas;
using Application.Sagas.Persistence;
using Domain.Entities;
using Infrastructure.Persistence.DbContext;
using Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Order.E2ETests.Infrastructure;

public static class E2ETestServerExtensions
{
    public static async Task<List<DomainEvent>> GetEventsAsync(
        this E2ETestServer server,
        Guid aggregateId,
        string aggregateType)
    {
        using var scope = server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.DomainEvents
            .Where(e => e.AggregateId == aggregateId.ToString()
                        && e.AggregateType == aggregateType)
            .OrderBy(e => e.Version)
            .ToListAsync();
    }

    public static async Task<SagaState?> GetSagaStateAsync(
        this E2ETestServer server,
        Guid correlationId,
        string sagaType)
    {
        using var scope = server.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISagaRepository>();
        return await repo.GetByCorrelationIdAsync(correlationId, sagaType, CancellationToken.None);
    }

    public static async Task<OrderResponse?> GetOrderReadModelAsync(
        this E2ETestServer server,
        Guid orderId)
    {
        using var scope = server.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOrderReadRepository>();
        return await repo.GetByIdAsync(orderId, CancellationToken.None);
    }

    public static async Task<ReturnRequestReadModel?> GetReturnReadModelAsync(
        this E2ETestServer server,
        Guid returnRequestId)
    {
        using var scope = server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.ReturnRequestReadModels
            .FirstOrDefaultAsync(r => r.Id == returnRequestId);
    }

    public static async Task<SagaState?> WaitForSagaStatusAsync(
        this E2ETestServer server,
        Guid correlationId,
        string sagaType,
        SagaStatus expectedStatus,
        int timeoutSeconds = 30)
    {
        for (var i = 0; i < timeoutSeconds; i++)
        {
            var state = await server.GetSagaStateAsync(correlationId, sagaType);
            if (state?.Status == expectedStatus)
                return state;

            await Task.Delay(1000);
        }

        return null;
    }

    public static async Task<OrderResponse?> WaitForReadModelStatusAsync(
        this E2ETestServer server,
        Guid orderId,
        string expectedStatus,
        int timeoutSeconds = 30)
    {
        for (var i = 0; i < timeoutSeconds; i++)
        {
            var model = await server.GetOrderReadModelAsync(orderId);
            if (model?.Status == expectedStatus)
                return model;

            await Task.Delay(1000);
        }

        return null;
    }

    public static async Task<ReturnRequestReadModel?> WaitForReturnReadModelStatusAsync(
        this E2ETestServer server,
        Guid returnRequestId,
        string expectedStatus,
        int timeoutSeconds = 30)
    {
        for (var i = 0; i < timeoutSeconds; i++)
        {
            var model = await server.GetReturnReadModelAsync(returnRequestId);
            if (model?.Status == expectedStatus)
                return model;

            await Task.Delay(1000);
        }

        return null;
    }
}
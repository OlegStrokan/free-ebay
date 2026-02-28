using Application.Interfaces;
using Application.Sagas;
using Application.Sagas.Persistence;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Persistence.DbContext;
using Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Domain.Interfaces;

namespace Order.E2ETests.Infrastructure;

public static class E2ETestServerExtensions
{
    public static async Task<List<IDomainEvent>> GetEventsAsync(
        this E2ETestServer server,
        Guid aggregateId,
        string aggregateType)
    {
        using var scope = server.Services.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStoreRepository>();

        var events = await eventStore.GetEventsAsync(
            aggregateId.ToString(), aggregateType, CancellationToken.None);

        return events.ToList();
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
        var db = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

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
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

    public static async Task<List<SagaStepLog>> GetSagaStepLogsAsync(
        this E2ETestServer server,
        Guid sagaId)
    {
        using var scope = server.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISagaRepository>();
        return await repo.GetStepLogsAsync(sagaId, CancellationToken.None);
    }

    public static async Task SaveSagaStateAsync(
        this E2ETestServer server,
        SagaState saga)
    {
        using var scope = server.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISagaRepository>();
        await repo.SaveAsync(saga, CancellationToken.None);
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

    public static async Task<B2BOrderReadModel?> GetB2BOrderReadModelAsync(
        this E2ETestServer server,
        Guid b2bOrderId)
    {
        using var scope = server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReadDbContext>();
        return await db.B2BOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == b2bOrderId);
    }

    public static async Task<B2BOrderReadModel?> WaitForB2BOrderReadModelStatusAsync(
        this E2ETestServer server,
        Guid b2bOrderId,
        string expectedStatus,
        int timeoutSeconds = 30)
    {
        for (var i = 0; i < timeoutSeconds; i++)
        {
            var model = await server.GetB2BOrderReadModelAsync(b2bOrderId);
            if (model?.Status == expectedStatus)
                return model;

            await Task.Delay(1000);
        }

        return null;
    }

    public static async Task<RecurringOrderReadModel?> GetRecurringOrderReadModelAsync(
        this E2ETestServer server,
        Guid recurringOrderId)
    {
        using var scope = server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReadDbContext>();
        return await db.RecurringOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == recurringOrderId);
    }

    public static async Task<RecurringOrderReadModel?> WaitForRecurringOrderReadModelStatusAsync(
        this E2ETestServer server,
        Guid recurringOrderId,
        string expectedStatus,
        int timeoutSeconds = 30)
    {
        for (var i = 0; i < timeoutSeconds; i++)
        {
            var model = await server.GetRecurringOrderReadModelAsync(recurringOrderId);
            if (model?.Status == expectedStatus)
                return model;

            await Task.Delay(1000);
        }

        return null;
    }
}
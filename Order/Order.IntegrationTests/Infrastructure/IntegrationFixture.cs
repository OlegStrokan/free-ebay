using Application.Interfaces;
using Application.Sagas.Persistence;
using Domain.Interfaces;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.EventIdempotencyChecker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace Order.IntegrationTests.Infrastructure;

/* Shared fixture that starts one PostgreSQL TestContainer per test class
 * model first, no migration needed
 * test are fully isolated without truncating tables - unique aggregate id per test
 * achieve the same isolation with less overhead
 */
public sealed class IntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("orderdb_integration")
        .WithUsername("test")
        .WithPassword("test")
        .WithImage("postgres:16-alpine")
        .Build();

    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var services = new ServiceCollection();

        // disable repository INFO logs in test output. keep WARNING and higher visible.
        services.AddLogging(b => b
            .AddConsole()
            .SetMinimumLevel(LogLevel.Warning));

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(_postgres.GetConnectionString()));

        services.AddScoped<IEventStoreRepository, EventStoreRepository>();
        services.AddScoped<ISnapshotRepository, SnapshotRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IDeadLetterRepository, DeadLetterRepository>();
        services.AddScoped<IOrderPersistenceService, OrderPersistenceService>();
        services.AddScoped<IReturnRequestPersistenceService, ReturnRequestPersistenceService>();
        services.AddScoped<ISagaRepository, SagaRepository>();
        services.AddScoped<OrderReadModelUpdater>();
        services.AddScoped<ReturnRequestReadModelUpdater>();
        services.AddScoped<IEventIdempotencyChecker, EventIdempotencyChecker>();

        Services = services.BuildServiceProvider();
        
        // build a full schema
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (Services is IAsyncDisposable d)
            await d.DisposeAsync();

        await _postgres.DisposeAsync();
    }
    
    public AsyncServiceScope CreateScope() => Services.CreateAsyncScope();
}

using Application.Interfaces;
using Application.Sagas;
using Application.Sagas.Persistence;
using Domain.Interfaces;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.EventIdempotencyChecker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace Order.IntegrationTests.Infrastructure;

/* Shared fixture that starts one PostgreSQL + one Redis TestContainer per test class.
 * model first, no migration needed.
 * Tests are fully isolated without truncating tables - unique aggregate id per test.
 */
public sealed class IntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("orderdb_integration")
        .WithUsername("test")
        .WithPassword("test")
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        var services = new ServiceCollection();

        // disable repository INFO logs in test output. keep WARNING and higher visible.
        services.AddLogging(b => b
            .AddConsole()
            .SetMinimumLevel(LogLevel.Warning));

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(_postgres.GetConnectionString()));

        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(_redis.GetConnectionString()));
        services.AddSingleton<ISagaDistributedLock, RedisSagaDistributedLock>();

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

        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
    
    public AsyncServiceScope CreateScope() => Services.CreateAsyncScope();
}

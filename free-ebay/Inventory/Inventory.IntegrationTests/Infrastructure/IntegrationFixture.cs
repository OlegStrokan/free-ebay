using Application.Interfaces;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Inventory.IntegrationTests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace Inventory.IntegrationTests.Infrastructure;

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationFixture>
{
}

public sealed class IntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder()
        .WithDatabase("inventorydb_integration")
        .WithUsername("test")
        .WithPassword("test")
        .WithImage("postgres:16-alpine")
        .Build();

    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();

        var services = new ServiceCollection();

        services.AddLogging(builder => builder
            .AddConsole()
            .SetMinimumLevel(LogLevel.Warning));

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(postgres.GetConnectionString()));

        services.AddSingleton<IOptions<KafkaOptions>>(Options.Create(new KafkaOptions
        {
            BootstrapServers = "localhost:9092",
            InventoryEventsTopic = "inventory.events",
            ClientId = "inventory-integration-tests"
        }));

        services.AddSingleton<FakeOutboxPublisher>();
        services.AddSingleton<IOutboxPublisher>(sp => sp.GetRequiredService<FakeOutboxPublisher>());

        services.AddScoped<IInventoryReservationStore, InventoryReservationStore>();

        Services = services.BuildServiceProvider();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (Services is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();

        await postgres.DisposeAsync();
    }

    public AsyncServiceScope CreateScope() => Services.CreateAsyncScope();

    public FakeOutboxPublisher GetPublisher() =>
        Services.GetRequiredService<FakeOutboxPublisher>();
}

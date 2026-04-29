using Application.Interfaces;
using Domain.Interfaces;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Product.IntegrationTests.TestHelpers;
using Testcontainers.PostgreSql;
using Xunit;

namespace Product.IntegrationTests.Infrastructure;

public sealed class IntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("productdb_integration")
        .WithUsername("test")
        .WithPassword("test")
        .WithImage("postgres:16-alpine")
        .Build();

    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var services = new ServiceCollection();

        services.AddLogging(b => b
            .AddConsole()
            .SetMinimumLevel(LogLevel.Warning));

        services.AddDbContext<ProductDbContext>(opt =>
            opt.UseNpgsql(_postgres.GetConnectionString()));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICatalogItemRepository, CatalogItemRepository>();
        services.AddScoped<IListingRepository, ListingRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IListingReadRepository, ListingReadRepository>();
        services.AddScoped<IProductPersistenceService, ProductPersistenceService>();

        // FakeEventPublisher is used directly in OutboxProcessorTests
        // not registered globally - tests create it themselves

        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
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

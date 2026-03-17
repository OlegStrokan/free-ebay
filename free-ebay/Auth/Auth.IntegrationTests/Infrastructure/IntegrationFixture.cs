using Domain.Repositories;
using Infrastructure.DbContext;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace Auth.IntegrationTests.Infrastructure;

public sealed class IntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("authdb_integration")
        .WithUsername("test")
        .WithPassword("test")
        .WithImage("postgres:16-alpine")
        .Build();

    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var services = new ServiceCollection();

        services.AddLogging(builder => builder
            .AddConsole()
            .SetMinimumLevel(LogLevel.Warning));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_postgres.GetConnectionString()));

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();

        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (Services is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    public AsyncServiceScope CreateScope() => Services.CreateAsyncScope();
}

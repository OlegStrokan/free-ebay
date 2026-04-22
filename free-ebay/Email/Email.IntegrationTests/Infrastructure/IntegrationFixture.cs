using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Email.Options;
using Email.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace Email.IntegrationTests.Infrastructure;

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationFixture>
{
}

public sealed class IntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("email_integration")
        .WithUsername("test")
        .WithPassword("test")
        .WithImage("postgres:16-alpine")
        .Build();

    // MailHog: SMTP on 1025, HTTP API on 8025
    private readonly IContainer _mailhog = new ContainerBuilder()
        .WithImage("mailhog/mailhog:latest")
        .WithPortBinding(1025, assignRandomHostPort: true)
        .WithPortBinding(8025, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8025))
        .Build();

    public IServiceProvider Services { get; private set; } = null!;

    public string MailHogApiBaseUrl { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _mailhog.StartAsync());

        MailHogApiBaseUrl = $"http://localhost:{_mailhog.GetMappedPublicPort(8025)}";

        var smtpPort = _mailhog.GetMappedPublicPort(1025);

        var services = new ServiceCollection();

        services.AddLogging(b => b
            .AddConsole()
            .SetMinimumLevel(LogLevel.Warning));

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString()
            })
            .Build());

        services.Configure<EmailDeliveryOptions>(o =>
        {
            o.SmtpHost = "localhost";
            o.SmtpPort = smtpPort;
            o.EnableSsl = false;
            o.Username = string.Empty;
            o.Password = string.Empty;
            o.DefaultFromAddress = "no-reply@free-ebay.com";
        });

        services.AddSingleton<IProcessedMessageStore, PostgresProcessedMessageStore>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        Services = services.BuildServiceProvider();

        // ensure the idempotency table exists
        var store = Services.GetRequiredService<IProcessedMessageStore>();
        await store.InitializeAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        if (Services is IAsyncDisposable d) await d.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _mailhog.DisposeAsync().AsTask());
    }
}

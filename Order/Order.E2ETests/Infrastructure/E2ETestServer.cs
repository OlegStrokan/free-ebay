using Application.Gateways;
using Confluent.Kafka;
using DotNet.Testcontainers.Builders;
using Grpc.Core;
using Grpc.Net.Client;
using Infrastructure.Messaging;
using Infrastructure.Persistence.DbContext;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using WireMock.Server;
using Xunit;

// e2e test server with real infra (postgresql, kafka, wireMock);
// uses testContainer for isolated test environment;

namespace Order.E2ETests.Infrastructure;

public class E2ETestServer : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer _postgreSqlContainer;
    private KafkaContainer _kafkaContainer;
    private WireMockServer _wireMockServer;

    public E2ETestServer(PostgreSqlContainer postgreSqlContainer, KafkaContainer kafkaContainer, WireMockServer wireMockServer)
    {
        _postgreSqlContainer = postgreSqlContainer;
        _kafkaContainer = kafkaContainer;
        _wireMockServer = wireMockServer;
    }

    public string PostgresConnectionString { get; private set; } = string.Empty;
    public string KafkaBootstrapServers { get; private set; } = string.Empty;
    public string WireMockUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithDatabase("orderdb_test")
            .WithUsername("test")
            .WithPassword("admin")
            .WithImage("postgres:16-alpine")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        await _postgreSqlContainer.StartAsync();
        PostgresConnectionString = _postgreSqlContainer.GetConnectionString();

        _kafkaContainer = new KafkaBuilder()
            .WithImage("confluentic/cp-kafka:7.6.0")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(9092))
            .Build();

        await _kafkaContainer.StartAsync();
        KafkaBootstrapServers = _kafkaContainer.GetBootstrapAddress();

        _wireMockServer = WireMockServer.Start();
        WireMockUrl = _wireMockServer.Urls[0];
        
        Console.WriteLine($"Test Infrastructure Stared:");
        Console.WriteLine($"PostgreSQL: {PostgresConnectionString}");
        Console.WriteLine($"Kafka: {KafkaBootstrapServers}");
        Console.WriteLine($"WireMock: {WireMockUrl}");
    }

    public override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(PostgresConnectionString);
            });

            services.Configure<KafkaOptions>(options =>
            {
                options.BootstrapSettings = KafkaBootstrapServers;
            });

            services.RemoveAll<IPaymentGateway>();

            // @todo: add gateway mock implmentations

        });
    }

    public T CreateGrpcClient<T>() where T : ClientBase<T>
    {
        var channel = GrpcChannel.ForAddress(
            Server.BaseAddress,
            new GrpcChannelOptions() { HttpClient = CreateClient() });

        return (T)Activator.CreateInstance(typeof(T), channel);
    }

    public T GetRequiredService<T>() where T : notnull
    {
        return Services.CreateScope().ServiceProvider.GetRequiredService<T>();
    }

    public async Task MigrateDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public void ResetWireMock()
    {
        _wireMockServer?.Reset();
    }

    public WireMockServer GetWireMockServer()
    {
        return _wireMockServer ?? throw new InvalidOperationException("WireMock not initialized");
    }

    public async Task DisposeAsync()
    {
        if (_postgreSqlContainer != null)
            await _postgreSqlContainer.DisposeAsync();
        if (_kafkaContainer != null)
            await _kafkaContainer.DisposeAsync();
        
        _wireMockServer?.Stop();
        _wireMockServer?.Dispose();

        await base.DisposeAsync();
    }
}
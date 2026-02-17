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
using Order.E2ETests.Infrastructure.Mocks;
using ProtoBuf.Meta;
using Protos.Order;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using WireMock.Server;
using Xunit;

// e2e test server with real infra (postgresql, kafka, wireMock);
// uses testContainer for isolated test environment;

namespace Order.E2ETests.Infrastructure;

public class E2ETestServer : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer _postgreSqlContainer = null!;
    private KafkaContainer _kafkaContainer = null!;
    private WireMockServer _wireMockServer = null!;

    private FakePaymentGrpcServer _paymentService = null!;
    private FakeInventoryGrpcServer _inventoryService = null!;
    private FakeAccountingGrpcServer _accountingService = null!;

    public FakePaymentGrpcServer PaymentService => _paymentService;
    public FakeInventoryGrpcServer InventoryService => _inventoryService;
    public FakeAccountingGrpcServer Acccounting => _accountingService;
    public WireMockServer Shipping => _wireMockServer;

    public string KafkaBootstrapServers { get; private set; } = string.Empty;
    

    public async Task InitializeAsync()
    {
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithDatabase("orderdb_test")
            .WithUsername("test")
            .WithPassword("admin")
            .WithImage("postgres:16-alpine")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
        
        _kafkaContainer = new KafkaBuilder()
            .WithImage("confluentic/cp-kafka:7.6.0")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(9092))
            .Build();

        await Task.WhenAll(
            _postgreSqlContainer.StartAsync(),
            _kafkaContainer.StartAsync());

        KafkaBootstrapServers = _kafkaContainer.GetBootstrapAddress();

        _wireMockServer = WireMockServer.Start();
        _paymentService = new FakePaymentGrpcServer();
        _inventoryService = new FakeInventoryGrpcServer();
        _accountingService = new FakeAccountingGrpcServer();
        
        Console.WriteLine("✅ E2E infra ready");
        Console.WriteLine($"   Postgres   : {_postgreSqlContainer.GetConnectionString()}");
        Console.WriteLine($"   Kafka      : {KafkaBootstrapServers}");
        Console.WriteLine($"   Shipping   : {_wireMockServer.Urls[0]}  ← WireMock REST");
        Console.WriteLine($"   Payment    : {_paymentService.Address}");
        Console.WriteLine($"   Inventory  : {_inventoryService.Address}");
        Console.WriteLine($"   Accounting : {_accountingService.Address}");

    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {

        builder.UseEnvironment("test");
        
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_postgreSqlContainer.GetConnectionString());
            });

            services.Configure<KafkaOptions>(options =>
            {
                options.BootstrapSettings = KafkaBootstrapServers;
            });

            services.RemoveAll<IPaymentGateway>();
            services.AddScoped<IPaymentGateway>(_ =>
                new FakeGrpcPaymentGateway(_paymentService.Address));
            
            services.RemoveAll<IInventoryGateway>();
            services.AddScoped<IInventoryGateway>(_ =>
                new FakeGrpcInventoryGateway(_inventoryService.Address));
            
            services.RemoveAll<IAccountingGateway>();
            services.AddScoped<IAccountingGateway>(_ =>
                new FakeGrpcAccountingGateway(_inventoryService.Address));
            
            services.RemoveAll<IPaymentGateway>();
            services.AddScoped<IPaymentGateway>(_ =>
                new FakeGrpcPaymentGateway(_wireMockServer.Urls[0]));
            
            // email-service: not replaced, IEmailGateway published to kafka, tests verify the kafka message
        });
    }

    //---------------helpers---------------//

    public OrderService.OrderServiceClient CreateOrderClient()
    {
        var httpClient = CreateClient();
        var channel = GrpcChannel.ForAddress(
            httpClient.BaseAddress!,
            new GrpcChannelOptions { HttpClient = httpClient });
        return new OrderService.OrderServiceClient(channel);
    }

    public T GetService<T>() where T : notnull
    {
        using var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    public async Task MigrateDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public void ResetAll()
    {
        _paymentService.Reset();
        _accountingService.Reset();
        _inventoryService.Reset();
        _wireMockServer.Reset();
    }

    public new async Task DisposeAsync()
    {
        await _postgreSqlContainer.DisposeAsync();
        await _kafkaContainer.DisposeAsync();
        await _paymentService.DisposeAsync();
        await _inventoryService.DisposeAsync();
        await _accountingService.DisposeAsync();
        _wireMockServer.Dispose();
        await base.DisposeAsync();
    }
}
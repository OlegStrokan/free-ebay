using Application.Gateways;
using Application.Interfaces;
using Application.Sagas;
using Application.Sagas.Persistence;
using Domain.Interfaces;
using Infrastructure.BackgroundServices;
using Infrastructure.Gateways;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.EventIdempotencyChecker;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Infrastructure;

public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<ISagaDistributedLock, RedisSagaDistributedLock>();

        // db context
        services.AddDbContext<AppDbContext>(opt => 
            opt.UseNpgsql(configuration.GetConnectionString("Postgres")));
        
        // Repositories
        services.AddScoped<IEventStoreRepository, EventStoreRepository>();
        services.AddScoped<ISnapshotRepository, SnapshotRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IDeadLetterRepository, DeadLetterRepository>();
        services.AddScoped<ISagaRepository, SagaRepository>();
        services.AddScoped<IOrderReadRepository, OrderReadRepository>();
        services.AddScoped<IReturnRequestLookupRepository, ReturnRequestLookupRepository>();

        // Services
        services.AddScoped<IOrderPersistenceService, OrderPersistenceService>();
        services.AddScoped<IReturnRequestPersistenceService, ReturnRequestPersistenceService>();
        services.AddScoped<ISagaErrorClassifier, PostgresSagaErrorClassifier>();
        services.AddScoped<ISagaHandlerFactory, SagaHandlerFactory>();
        services.AddScoped<IEventPublisher, KafkaEventPublisher>();
        services.AddScoped<IEventIdempotencyChecker, EventIdempotencyChecker>();

        // Gateways
        services.AddScoped<IInventoryGateway, InventoryGateway>();
        services.AddScoped<IPaymentGateway, PaymentGateway>();
        services.AddScoped<IShippingGateway, ShippingGateway>();
        services.AddScoped<IAccountingGateway, AccountingGateway>();
        services.AddScoped<IEmailGateway, EmailGateway>();
        services.AddScoped<IIncidentReporter, HelpDeskIncidentReporter>();

        // Background services
        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<SagaOrchestrationService>();
        services.AddHostedService<KafkaReadModelSynchronizer>();
        services.AddHostedService<SagaWatchdogService>();

        return services;
        
    }
}
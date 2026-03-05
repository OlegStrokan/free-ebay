using Application.Gateways;
using Application.Interfaces;
using Application.Sagas;
using Application.Sagas.Persistence;
using Confluent.Kafka;
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

        services.AddSingleton<IConsumer<string, string>>(sp =>
        {
            var kafkaConfig = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                GroupId           = configuration["Kafka:ConsumerGroupId"] ?? "order-service",
                AutoOffsetReset   = AutoOffsetReset.Earliest,
                EnableAutoCommit  = false,
                EnableAutoOffsetStore = false,
                IsolationLevel    = IsolationLevel.ReadCommitted
            };
            return new ConsumerBuilder<string, string>(kafkaConfig)
                .SetErrorHandler((_, error) =>
                {
                    var logger = sp.GetRequiredService<ILogger<SagaOrchestrationService>>();
                    logger.LogError("Kafka saga consumer error: {Reason}", error.Reason);
                })
                .Build();
        });

        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<ISagaDistributedLock, RedisSagaDistributedLock>();
        services.AddSingleton<IDomainEventTypeRegistry, DomainEventTypeRegistry>();

        // db context
        services.AddDbContext<AppDbContext>(opt => 
            opt.UseNpgsql(configuration.GetConnectionString("Postgres")));
        
        services.AddDbContext<ReadDbContext>(opt =>
            opt.UseNpgsql(
                configuration.GetConnectionString("PostgresReadModel")
                ?? configuration.GetConnectionString("Postgres")));
        
        // Repositories
        services.AddScoped<IEventStoreRepository, EventStoreRepository>();
        services.AddScoped<ISnapshotRepository, SnapshotRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IDeadLetterRepository, DeadLetterRepository>();
        services.AddScoped<ISagaRepository, SagaRepository>();
        services.AddScoped<IOrderReadRepository, OrderReadRepository>();
        services.AddScoped<IReturnRequestLookupRepository, ReturnRequestLookupRepository>();
        services.AddScoped<IB2BOrderReadRepository, B2BOrderReadRepository>();
        services.AddScoped<IRecurringOrderReadRepository, RecurringOrderReadRepository>();

        // Services
        services.AddScoped<IOrderPersistenceService, OrderPersistenceService>();
        services.AddScoped<IReturnRequestPersistenceService, ReturnRequestPersistenceService>();
        services.AddScoped<IB2BOrderPersistenceService, B2BOrderPersistenceService>();
        services.AddScoped<IRecurringOrderPersistenceService, RecurringOrderPersistenceService>();
        services.AddScoped<ISagaErrorClassifier, PostgresSagaErrorClassifier>();
        services.AddScoped<ISagaHandlerFactory, SagaHandlerFactory>();
        services.AddScoped<IEventPublisher, KafkaEventPublisher>();
        services.AddScoped<IEventIdempotencyChecker, EventIdempotencyChecker>();
        services.AddSingleton<IReadModelHandlerRegistry, ReadModelHandlerRegistry>();
        services.AddScoped<IReadModelUpdater, OrderReadModelUpdater>();
        services.AddScoped<IReadModelUpdater, ReturnRequestReadModelUpdater>();
        // register as concrete (for direct test resolution) AND as IReadModelUpdater (for routing)
        services.AddScoped<B2BOrderReadModelUpdater>();
        services.AddScoped<IReadModelUpdater>(sp => sp.GetRequiredService<B2BOrderReadModelUpdater>());
        services.AddScoped<RecurringOrderReadModelUpdater>();
        services.AddScoped<IReadModelUpdater>(sp => sp.GetRequiredService<RecurringOrderReadModelUpdater>());

        // Gateways
        services.AddScoped<IInventoryGateway, InventoryGateway>();
        services.AddScoped<IPaymentGateway, PaymentGateway>();
        services.AddScoped<IShippingGateway, ShippingGateway>();
        services.AddScoped<IAccountingGateway, AccountingGateway>();
        services.AddScoped<IEmailGateway, EmailGateway>();
        services.AddScoped<IIncidentReporter, HelpDeskIncidentReporter>();

        // Background services
        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<RecurringOrderSchedulerService>();
        services.AddHostedService<SagaOrchestrationService>();
        services.AddHostedService<KafkaReadModelSynchronizer>();
        services.AddHostedService<SagaWatchdogService>();
        services.AddHostedService<ProcessedEventsCleanupService>();

        return services;
        
    }
}
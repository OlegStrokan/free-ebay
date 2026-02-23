using Application.Gateways;
using Application.Interfaces;
using Application.Sagas.Persistence;
using Domain.Interfaces;
using Infrastructure.BackgroundServices;
using Infrastructure.Gateways;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        // db context
        services.AddDbContext<AppDbContext>(opt => 
            opt.UseNpgsql(configuration.GetConnectionString("Postgres")));
        
        // Repositories
        services.AddScoped<IEventStoreRepository, EventStoreRepository>();
        // services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReturnRequestRepository, ReturnRequestRepository>();
        services.AddScoped<ISnapshotRepository, SnapshotRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IDeadLetterRepository, DeadLetterRepository>();
        services.AddScoped<ISagaRepository, SagaRepository>();
        services.AddScoped<IOrderReadRepository, OrderReadRepository>();

        // Services
        services.AddScoped<IOrderPersistenceService, OrderPersistenceService>();
        services.AddScoped<IReturnRequestPersistenceService, ReturnRequestPersistenceService>();
        services.AddScoped<ISagaErrorClassifier, PostgresSagaErrorClassifier>();
        services.AddScoped<ISagaHandlerFactory, SagaHandlerFactory>();
        services.AddScoped<IEventPublisher, KafkaEventPublisher>();

        // Gateways
        services.AddScoped<IInventoryGateway, InventoryGateway>();
        services.AddScoped<IPaymentGateway, PaymentGateway>();
        services.AddScoped<IShippingGateway, ShippingGateway>();
        services.AddScoped<IAccountingGateway, AccountingGateway>();
        // services.AddScoped<IEmailGateway, EmailGateway>();

        // Background services
        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<SagaOrchestrationService>();
        services.AddHostedService<KafkaReadModelSynchronizer>();
        services.AddHostedService<SagaWatchdogService>();

        return services;
        
    }
}
using Application.Interfaces;
using Domain.Interfaces;
using Infrastructure.BackgroundServices;
using Infrastructure.Messaging;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

        services.AddDbContext<ProductDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICatalogItemRepository, CatalogItemRepository>();
        services.AddScoped<IListingRepository, ListingRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IListingReadRepository, ListingReadRepository>();
        services.AddScoped<IProductReadRepository, ProductReadRepository>();

        services.AddScoped<IProductPersistenceService, ProductPersistenceService>();

        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<ProcessedOutboxCleanupService>();
        services.AddHostedService<InventoryKafkaConsumerBackgroundService>();

        return services;
    }
}

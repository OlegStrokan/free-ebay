using Application.Consumers;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ApplicationModule
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IProductEventConsumer, ProductCreatedConsumer>();
        services.AddScoped<IProductEventConsumer, ProductUpdatedConsumer>();
        services.AddScoped<IProductEventConsumer, ProductDeletedConsumer>();
        services.AddScoped<IProductEventConsumer, ProductStockUpdatedConsumer>();
        services.AddScoped<IProductEventConsumer, ProductStatusChangedConsumer>();
        services.AddScoped<IProductEventConsumer, CatalogItemCreatedConsumer>();
        services.AddScoped<IProductEventConsumer, CatalogItemUpdatedConsumer>();
        services.AddScoped<IProductEventConsumer, CatalogItemListingSummaryUpdatedConsumer>();

        return services;
    }
}

using Application.Common;
using Application.Consumers;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ApplicationModule
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationModule).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(ApplicationModule).Assembly);

        services.AddScoped<IInventoryEventConsumer, InventoryConfirmedConsumer>();
        services.AddScoped<IInventoryEventConsumer, InventoryReleasedConsumer>();
        services.AddScoped<IInventoryEventConsumer, InventoryExpiredConsumer>();

        return services;
    }
}

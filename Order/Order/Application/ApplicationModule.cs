using Application.Sagas.Handlers;
using Application.Sagas.OrderSaga;
using Application.Sagas.OrderSaga.Steps;
using Application.Sagas.ReturnSaga;
using Application.Sagas.ReturnSaga.Steps;
using Application.Sagas.Steps;
using Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;

namespace Application;

public static class ApplicationModule
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // domain services (registered here because application => domain reference already exists)
        services.AddSingleton<ReturnPolicyService>();
        
        // command/query handlers
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationModule).Assembly));
        
        // validators
        services.AddValidatorsFromAssembly(typeof(ApplicationModule).Assembly);
        
        // sagas
        services.AddScoped<IOrderSaga, OrderSaga>();
        services.AddScoped<IReturnSaga, ReturnSaga>();
        
        // order saga steps
        services.AddScoped<ISagaStep<OrderSagaData, OrderSagaContext>, ReserveInventoryStep>();
        services.AddScoped<ISagaStep<OrderSagaData, OrderSagaContext>, ProcessPaymentStep>();
        services.AddScoped<ISagaStep<OrderSagaData, OrderSagaContext>, UpdateOrderStatusStep>();
        services.AddScoped<ISagaStep<OrderSagaData, OrderSagaContext>, CreateShipmentStep>();
        services.AddScoped<ISagaStep<OrderSagaData, OrderSagaContext>, SendConfirmationEmailStep>();
        
        // return saga step
        services.AddScoped<ISagaStep<ReturnSagaData, ReturnSagaContext>, ValidateReturnRequestStep>();
        services.AddScoped<ISagaStep<ReturnSagaData, ReturnSagaContext>, AwaitReturnShipmentStep>();
        services.AddScoped<ISagaStep<ReturnSagaData, ReturnSagaContext>, ConfirmReturnReceivedStep>();
        services.AddScoped<ISagaStep<ReturnSagaData, ReturnSagaContext>, CompleteReturnStep>();
        services.AddScoped<ISagaStep<ReturnSagaData, ReturnSagaContext>, ProcessRefundStep>();
        services.AddScoped<ISagaStep<ReturnSagaData, ReturnSagaContext>, UpdateAccountingRecordsStep>();

        // saga event handlers - consumed by kafka consumer
        services.AddScoped<ISagaEventHandler, OrderCreatedEventHandler>();
        services.AddScoped<ISagaEventHandler, ReturnRequestCreatedEventHandler>();
        services.AddScoped<ISagaEventHandler, ReturnShipmentDeliveredEventHandler>();

        // lightweight type-only descriptors used by SagaHandlerFactory (singleton).
        // these carry just EventType string + handler .NET Type - no DI chains involved.
        services.AddSingleton(new SagaHandlerDescriptor("OrderCreatedEvent",              typeof(OrderCreatedEventHandler)));
        services.AddSingleton(new SagaHandlerDescriptor("ReturnRequestCreatedEvent",      typeof(ReturnRequestCreatedEventHandler)));
        services.AddSingleton(new SagaHandlerDescriptor("ReturnShipmentDeliveredEvent",   typeof(ReturnShipmentDeliveredEventHandler)));

        return services;
    }
}
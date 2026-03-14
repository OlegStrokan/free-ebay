using Application.Interfaces;
using Application.Sagas.Handlers;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Infrastructure.Tests.Services;

public class SagaHandlerFactoryTests
{
    // Concrete classes so GetType() is stable and distinct - pure Substitute.For<ISagaEventHandler>()
    // creates the same DynamicProxy type for every substitution, which breaks the type-based dispatch.
    private sealed class OrderCreatedHandler : ISagaEventHandler
    {
        public string EventType => "OrderCreatedEvent";
        public string SagaType  => "OrderSaga";
        public Task HandleAsync(string eventPayload, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class OrderPaidHandler : ISagaEventHandler
    {
        public string EventType => "OrderPaidEvent";
        public string SagaType  => "OrderSaga";
        public Task HandleAsync(string eventPayload, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public void GetHandler_ShouldReturnHandler_WhenEventTypeIsRegistered()
    {
        var handler = new OrderCreatedHandler();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(OrderCreatedHandler)).Returns(handler);

        var factory = new SagaHandlerFactory(new ISagaEventHandler[] { handler });

        var result = factory.GetHandler(serviceProvider, "OrderCreatedEvent");

        Assert.Same(handler, result);
    }

    [Fact]
    public void GetHandler_ShouldReturnNull_WhenEventTypeIsNotRegistered()
    {
        var factory = new SagaHandlerFactory(Array.Empty<ISagaEventHandler>());

        var result = factory.GetHandler(Substitute.For<IServiceProvider>(), "UnknownEvent");

        Assert.Null(result);
    }

    [Fact]
    public void GetHandler_ShouldReturnCorrectHandler_WhenMultipleHandlersRegistered()
    {
        var h1 = new OrderCreatedHandler();
        var h2 = new OrderPaidHandler();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(OrderCreatedHandler)).Returns(h1);
        serviceProvider.GetService(typeof(OrderPaidHandler)).Returns(h2);

        var factory = new SagaHandlerFactory(new ISagaEventHandler[] { h1, h2 });

        var result1 = factory.GetHandler(serviceProvider, "OrderCreatedEvent");
        var result2 = factory.GetHandler(serviceProvider, "OrderPaidEvent");

        Assert.Same(h1, result1);
        Assert.Same(h2, result2);
    }

    [Fact]
    public void GetHandler_ShouldReturnNull_WhenHandlerTypeIsRegisteredInMappingButNotInServiceProvider()
    {
        var handler = new OrderCreatedHandler();

        // factory knows about OrderCreatedEvent => OrderCreatedHandler type
        var factory = new SagaHandlerFactory(new ISagaEventHandler[] { handler });

        // but the service provider returns nothing (not registered in DI)
        var serviceProvider = Substitute.For<IServiceProvider>();

        var result = factory.GetHandler(serviceProvider, "OrderCreatedEvent");

        Assert.Null(result);
    }
}

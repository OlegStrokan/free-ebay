using Application.Interfaces;
using Application.Sagas.Handlers;

namespace Infrastructure.Services;

// EventType -> handler .NET Type mapping built at startup, NOT per-scope.
// Previously the factory took IEnumerable<ISagaEventHandler> which forced the ENTIRE
// dependency graph of every saga handler (steps, gateways, repos) to be instantiated
// just to read a string property - causing DI resolution failures on every Kafka message
// when any leaf gateway/client was missing from the DI container.
//
// The descriptor-based constructor (used in production DI) avoids all of that.
// The handler-instance constructor is kept for unit tests that pass concrete stubs directly.
public class SagaHandlerFactory : ISagaHandlerFactory
{
    private readonly IReadOnlyDictionary<string, Type> _handlerMapping;

    // Production constructor: takes lightweight descriptors, no saga chains instantiated.
    // Register the factory as AddSingleton when using this path.
    public SagaHandlerFactory(IEnumerable<SagaHandlerDescriptor> descriptors)
    {
        _handlerMapping = descriptors.ToDictionary(
            d => d.EventType,
            d => d.HandlerType
        );
    }

    // Kept for unit tests that supply concrete ISagaEventHandler stubs directly.
    public SagaHandlerFactory(IEnumerable<ISagaEventHandler> handlers)
    {
        _handlerMapping = handlers.ToDictionary(
            h => h.EventType,
            h => h.GetType()
        );
    }

    public ISagaEventHandler? GetHandler(IServiceProvider serviceProvider, string eventType)
    {
        if (!_handlerMapping.TryGetValue(eventType, out var handleType))
            return null;

        // Resolve only the concrete type we need — avoids instantiating every other handler
        // (and their entire dependency chains) just to find one by type comparison.
        return serviceProvider.GetService(handleType) as ISagaEventHandler;
    }
}
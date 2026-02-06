using Application.Interfaces;
using Application.Sagas.Handlers;

namespace Infrastructure.Services;

public class SagaHandlerFactory : ISagaHandlerFactory
{
    private readonly IReadOnlyDictionary<string, Type> _handlerMapping;

    public SagaHandlerFactory(IEnumerable<ISagaEventHandler> handlers)
    {
        _handlerMapping = handlers.ToDictionary(
            h => h.EventType,
            h => h.GetType()
        );
    }

    public ISagaEventHandler? GetHandler(IServiceProvider serviceProvider, string eventType)
    {
        return _handlerMapping.TryGetValue(eventType, out var handlerType)
            ? (ISagaEventHandler)serviceProvider.GetRequiredService(handlerType)
            : null;
    }
}
using Application.Interfaces;
using Application.Sagas.Handlers;

namespace Infrastructure.Services;

// updated version: instead of looking for all services under the ISagaEventHandler
// then locally find correct implementation. It's OK our case, but if in future we will 
// process millions of events per seconds we probably need to find another solution
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
        if (!_handlerMapping.TryGetValue(eventType, out var handleType))
            return null;

        return serviceProvider.GetServices<ISagaEventHandler>()
            .FirstOrDefault(h => h.GetType() == handleType);
    }
}
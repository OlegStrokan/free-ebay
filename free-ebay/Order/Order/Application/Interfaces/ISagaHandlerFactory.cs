using Application.Sagas.Handlers;

namespace Application.Interfaces;

public interface ISagaHandlerFactory
{
    ISagaEventHandler? GetHandler(IServiceProvider serviceProvider, string eventType);
}
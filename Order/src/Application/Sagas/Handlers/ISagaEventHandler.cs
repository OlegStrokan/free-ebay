namespace Application.Sagas.Handlers;

public interface ISagaEventHandler
{
    string EventType { get; }
    string SagaType { get; }
    Task HandleAsync(string eventPayload, CancellationToken cancellationToken);
}
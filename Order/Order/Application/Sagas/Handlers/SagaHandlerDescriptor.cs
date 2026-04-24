namespace Application.Sagas.Handlers;

/// <summary>
/// Carries the EventType → handler .NET Type mapping without instantiating the handler.
/// Used by SagaHandlerFactory (singleton) to avoid eagerly constructing entire saga
/// dependency chains (saga steps, gateways, repositories) on every Kafka message scope.
/// </summary>
public sealed class SagaHandlerDescriptor
{
    public string EventType { get; }
    public Type HandlerType { get; }

    public SagaHandlerDescriptor(string eventType, Type handlerType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType must not be empty.", nameof(eventType));

        if (!typeof(ISagaEventHandler).IsAssignableFrom(handlerType))
            throw new ArgumentException(
                $"{handlerType.Name} must implement ISagaEventHandler.",
                nameof(handlerType));

        EventType = eventType;
        HandlerType = handlerType;
    }
}

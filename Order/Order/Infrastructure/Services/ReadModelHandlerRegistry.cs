using System.Collections.Concurrent;
using System.Reflection;
using Domain.Common;

namespace Infrastructure.Services;

/// <summary>
/// Cached handler registry for read model updaters.
/// Replaces reflection-based routing with dictionary lookups of pre-compiled delegates.
/// Built once at startup, used for every event.
/// </summary>
public interface IReadModelHandlerRegistry
{
    Task HandleAsync(IDomainEvent domainEvent, IReadModelUpdater updater, CancellationToken ct);
}

public sealed class ReadModelHandlerRegistry : IReadModelHandlerRegistry
{
    private readonly ConcurrentDictionary<(Type UpdaterType, Type EventType), 
        Func<IReadModelUpdater, IDomainEvent, CancellationToken, Task>> _handlerCache = new();
    
    private readonly ILogger<ReadModelHandlerRegistry> _logger;

    public ReadModelHandlerRegistry(ILogger<ReadModelHandlerRegistry> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(IDomainEvent domainEvent, IReadModelUpdater updater, CancellationToken ct)
    {
        var eventType = domainEvent.GetType();
        var updaterType = updater.GetType();

        var handler = _handlerCache.GetOrAdd((updaterType, eventType), key =>
        {
            var method = CompileHandler(key.UpdaterType, key.EventType);
            if (method == null)
            {
                return (u, e, c) => Task.CompletedTask;
            }
            return method;
        });

        await handler(updater, domainEvent, ct);
    }

    private Func<IReadModelUpdater, IDomainEvent, CancellationToken, Task>? CompileHandler(
        Type updaterType, 
        Type eventType)
    {
        //@todo: get rid of reflection
        // Find HandleAsync(EventType, CancellationToken) method
        var handleMethod = updaterType.GetMethod(
            "HandleAsync",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { eventType, typeof(CancellationToken) },
            modifiers: null);

        if (handleMethod == null)
        {
            _logger.LogWarning(
                "No HandleAsync({EventType}, CancellationToken) method found on {UpdaterType}",
                eventType.Name, updaterType.Name);
            return null;
        }

        // Compile to delegate: (updater, domainEvent, ct) => updater.HandleAsync((TEvent)domainEvent, ct)
        return (updater, domainEvent, ct) =>
        {
            var result = handleMethod.Invoke(updater, new object?[] { domainEvent, ct });
            return result is Task task ? task : Task.CompletedTask;
        };
    }
}

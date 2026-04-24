using System.Reflection;
using Domain.Common;

namespace Infrastructure.Services;

/// <summary>
/// it's just shared type shit what we have duplicated before
/// Discovers all <see cref="IDomainEvent"/> implementations at startup and exposes them
/// as a name → Type dictionary.  Registered as a singleton so the reflection walk happens
/// exactly once; both EventStoreRepository and KafkaReadModelSynchronizer consume it.
/// </summary>
public interface IDomainEventTypeRegistry
{
    bool TryGetType(string name, out Type type);
    IReadOnlyDictionary<string, Type> All { get; }
}

public sealed class DomainEventTypeRegistry : IDomainEventTypeRegistry
{
    private readonly Dictionary<string, Type> _map;

    public DomainEventTypeRegistry(ILogger<DomainEventTypeRegistry> logger)
    {
        _map = DiscoverEventTypes();
        logger.LogInformation(
            "Discovered {Count} domain event types: {Names}",
            _map.Count,
            string.Join(", ", _map.Keys));
    }

    public IReadOnlyDictionary<string, Type> All => _map;

    public bool TryGetType(string name, out Type type) => _map.TryGetValue(name, out type!);

    private static Dictionary<string, Type> DiscoverEventTypes()
    {
        var result = new Dictionary<string, Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.FullName?.StartsWith("System") == true ||
                assembly.FullName?.StartsWith("Microsoft") == true)
                continue;

            try
            {
                foreach (var type in assembly.GetTypes()
                    .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) &&
                                !t.IsInterface && !t.IsAbstract))
                {
                    result[type.Name] = type;
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // skip assemblies that fail to load
            }
        }

        return result;
    }
}

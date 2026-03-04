
using System.Collections.Concurrent;
using System.Reflection;

namespace Domain.Common;

// Events are stored in event store and replayed to rebuild state
public abstract class AggregateRoot<TId> : Entity<TId>
{
    private readonly List<IDomainEvent> _uncommitedEvents = new();
    public int Version { get; private set; } = 0;
    private static readonly ConcurrentDictionary<(Type, Type), MethodInfo> _applyMethodCache = new();

    public IReadOnlyList<IDomainEvent> UncommitedEvents => _uncommitedEvents.AsReadOnly();
    

    protected AggregateRoot(TId id ) : base(id) {}
    protected AggregateRoot() : base() {}


    protected void RaiseEvent(IDomainEvent @event)
    {
        ApplyEvent(@event);
        _uncommitedEvents.Add(@event);
    }

    protected void ApplyEvent(IDomainEvent @event)
    {
        var aggregateType = GetType();
        var eventType = @event.GetType();

        var applyMethod = _applyMethodCache.GetOrAdd(
            (aggregateType, eventType),
            key => key.Item1.GetMethod(
                "Apply",
                BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { key.Item2 },
                modifiers: null)
            ?? throw new InvalidOperationException(
                $"Missing Apply({key.Item2.Name}) method on {key.Item1.Name}. "
                + $"Add 'private void Apply({key.Item2.Name} evt)' to handle this event."));

        applyMethod.Invoke(this, new object[] { @event });
        Version++;
    }

    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        foreach (var @event in history)
        {
            ApplyEvent(@event);
        }
    }

    public void ClearUncommittedEvents()
    {
        _uncommitedEvents.Clear();
    }


    protected void RestoreVersion(int version)
    {
        Version = version;
    }
    
    
} 
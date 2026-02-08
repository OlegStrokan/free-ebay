
namespace Domain.Common;

// For Event Sourcing aggregates, no need for separate domain events collection
// Events are stored in event store and replayed to rebuild state
public abstract class AggregateRoot<TId> : Entity<TId>
{
    private readonly List<IDomainEvent> _uncommitedEvents = new();
    public int Version { get; private set; } = -1;

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
        ((dynamic)this).Apply((dynamic)@event);
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
} 
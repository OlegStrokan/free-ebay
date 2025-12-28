
namespace Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected AggregateRoot(TId id ) : base(id) {}
    
    protected AggregateRoot() : base() {}

    protected void AddDomainEvent(IDomainEvent newDomainEvent)
    {
        _domainEvents.Add(newDomainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
} 

namespace Domain.Common;

// For Event Sourcing aggregates, no need for separate domain events collection
// Events are stored in event store and replayed to rebuild state
public abstract class AggregateRoot<TId> : Entity<TId>
{
    protected AggregateRoot(TId id ) : base(id) {}
    
    protected AggregateRoot() : base() {}
} 
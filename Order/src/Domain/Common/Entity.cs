namespace Domain.Common;

//@think: why it's not a record?

public abstract class Entity<TId>
{
    public TId Id { get; protected set; }


    protected Entity(TId id)
    {
        Id = id;
    }
    
    protected Entity() {}

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) 
            return false;
        
        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Id?.Equals(other.Id) ?? false;
    }

    public override int GetHashCode()
    {
        return Id?.GetHashCode() ?? 0;
    }

    public static bool operator ==(Entity<TId>? a, Entity<TId>? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }

    public static bool operator !=(Entity<TId>? a, Entity<TId>? b)
    {
        return !(a == b);
    }
}
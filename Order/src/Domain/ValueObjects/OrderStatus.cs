namespace Domain.ValueObjects;

public sealed class OrderStatus
{
    public static readonly OrderStatus Pending = new("Pending", 0);
    public static readonly OrderStatus Paid = new("Paid", 1);
    public static readonly OrderStatus Approved = new("Approved", 2);
    public static readonly OrderStatus Completed = new("Completed", 3);
    public static readonly OrderStatus Cancelled = new("Cancelled", 4);
    
    public string Name { get; }
    public int Value { get; }
    private readonly HashSet<OrderStatus> _allowedTransitions;

    public IReadOnlyCollection<OrderStatus> AllowedTransitions => _allowedTransitions;
    
    private OrderStatus(string name, int value)
    {
        Name = name;
        Value = value;
        _allowedTransitions = new HashSet<OrderStatus>();
    }

    static OrderStatus()
    {
        Pending.AllowsTransitionTo(Paid, Cancelled);
        Paid.AllowsTransitionTo(Approved, Cancelled);
        Approved.AllowsTransitionTo(Completed);
        Completed.AllowsTransitionTo();
        Cancelled.AllowsTransitionTo();
    }

    private void AllowsTransitionTo(params OrderStatus[] targets)
    {
        foreach (var target in targets)
        {
            _allowedTransitions.Add(target);
        }
    }


    public bool CanTransitionTo(OrderStatus target)
    {
        return _allowedTransitions.Contains(target);
    }

    public void ValidateTransitionTo(OrderStatus target)
    {
        if (!CanTransitionTo(target))
        {
            throw new InvalidOperationException(
                $"Cannot transition from {Name} to {target.Name}. " +
                $"Allowed transition: {string.Join(", ", _allowedTransitions.Select(t => t.Name))}");
        }
    }
    
    // for persistence 
    public static OrderStatus FromValue(int value)
    {
        return value switch
        {
            0 => Pending,
            1 => Paid,
            2 => Approved,
            3 => Completed,
            4 => Cancelled,
            _ => throw new ArgumentException($"Unknown OrderStatus value: {value}", nameof(value))
        };
    }

    public static OrderStatus FromName(string name)
    {
        return name switch
        {
            "Pending" => Pending,
            "Paid" => Paid,
            "Approved" => Approved,
            "Completed" => Completed,
            "Cancelled" => Cancelled,
            _ => throw new ArgumentException($"Unknown OrderStatus name: {name}", nameof(name))
        };
    }

    public override string ToString() => Name;
    
    

    public bool CanAssignTracking() => this == Paid || this == Approved;

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is OrderStatus other && Value == other.Value;

    }

    public static bool operator ==(OrderStatus? left, OrderStatus? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(OrderStatus? left, OrderStatus? right) => !(left == right);


}
namespace Domain.ValueObjects;

public sealed class ReturnStatus
{
    public static readonly ReturnStatus Pending = new("Pending", 0);
    public static readonly ReturnStatus Received = new("Received", 1);
    public static readonly ReturnStatus Refunded = new("Refunded", 2);
    public static readonly ReturnStatus Completed = new("Completed", 3);
    
    public string Name { get; }
    public int Value { get;  }
    public readonly HashSet<ReturnStatus> _allowedTransitions;

    public HashSet<ReturnStatus> AllowedTransitions => _allowedTransitions;

    private ReturnStatus(string name, int value)
    {
        Name = name;
        Value = value;
        _allowedTransitions = new HashSet<ReturnStatus>();
    }

    static ReturnStatus()
    {
        Pending.AllowsTransitionTo(Received);
        Received.AllowsTransitionTo(Refunded);
        Refunded.AllowsTransitionTo(Completed);
        Completed.AllowsTransitionTo();
    }

    private void AllowsTransitionTo(params ReturnStatus[] targets)
    {
        foreach (var target in targets)
        {
            _allowedTransitions.Add(target);
        }
    }

    public bool CanTransitionTo(ReturnStatus target)
    {
        return _allowedTransitions.Contains(target);
    }

    public void ValidateTransitionTo(ReturnStatus target)
    {
        if (!CanTransitionTo(target))
        {
            throw new InvalidOperationException(
                $"Cannot transition from {Name} to {target.Name}. " +
                $"Allowed transition: {string.Join(", ", _allowedTransitions.Select(t => t.Name))}");
        }
    }
    
    // for persistence
    public static ReturnStatus FromValue(int value)
    {
        return value switch
        {
            0 => Pending,
            1 => Received,
            2 => Refunded,
            3 => Completed,
            _ => throw new ArgumentException($"Unknown ReturnStatus value: {value}", nameof(value))
        };
    }

    public static ReturnStatus FromName(string name)
    {
        return name switch
        {
            "Pending" => Pending,
            "Received" => Received,
            "Refunded" => Refunded,
            "Completed" => Completed,
            _ => throw new ArgumentException($"Unknown ReturnStatus name: {name}", nameof(name))
        };
    }
    
    
    public override bool Equals(object? obj)
    {
        return obj is ReturnStatus other && Value == other.Value;

    }

    public static bool operator ==(ReturnStatus? left, ReturnStatus? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(ReturnStatus? left, ReturnStatus? right) => !(left == right);


}
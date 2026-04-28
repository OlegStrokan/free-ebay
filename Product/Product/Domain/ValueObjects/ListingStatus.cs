using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed class ListingStatus
{
    public static readonly ListingStatus Active = new("Active", 1);
    public static readonly ListingStatus Inactive = new("Inactive", 2);
    public static readonly ListingStatus OutOfStock = new("OutOfStock", 3);
    public static readonly ListingStatus Deleted = new("Deleted", 4);

    private readonly HashSet<ListingStatus> _allowedTransitions = new();

    public string Name { get; }
    public int Value { get; }
    public IReadOnlyCollection<ListingStatus> AllowedTransitions => _allowedTransitions;

    private ListingStatus(string name, int value)
    {
        Name = name;
        Value = value;
    }

    static ListingStatus()
    {
        Active.AllowsTransitionTo(Inactive, OutOfStock, Deleted);
        Inactive.AllowsTransitionTo(Active, Deleted);
        OutOfStock.AllowsTransitionTo(Active, Deleted);
        Deleted.AllowsTransitionTo();
    }

    private void AllowsTransitionTo(params ListingStatus[] targets)
    {
        foreach (var target in targets)
            _allowedTransitions.Add(target);
    }

    public bool CanTransitionTo(ListingStatus target) => _allowedTransitions.Contains(target);

    public void ValidateTransitionTo(ListingStatus target)
    {
        if (!CanTransitionTo(target))
            throw new DomainException(
                $"Cannot transition from {Name} to {target.Name}. "
                + $"Allowed transitions: {string.Join(", ", _allowedTransitions.Select(t => t.Name))}");
    }

    public static ListingStatus FromValue(int value) => value switch
    {
        1 => Active,
        2 => Inactive,
        3 => OutOfStock,
        4 => Deleted,
        _ => throw new InvalidValueException($"Unknown ListingStatus value: {value}")
    };

    public static ListingStatus FromName(string name) => name switch
    {
        "Active" => Active,
        "Inactive" => Inactive,
        "OutOfStock" => OutOfStock,
        "Deleted" => Deleted,
        _ => throw new InvalidValueException($"Unknown ListingStatus name: {name}")
    };

    public override bool Equals(object? obj) => obj is ListingStatus other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Name;
}
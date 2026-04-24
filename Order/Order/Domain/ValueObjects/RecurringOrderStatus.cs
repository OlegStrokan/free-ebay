using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed class RecurringOrderStatus
{
    public static readonly RecurringOrderStatus Active = new("Active");
    public static readonly RecurringOrderStatus Paused = new("Paused");
    public static readonly RecurringOrderStatus Cancelled = new("Cancelled");

    public string Name { get; }

    private RecurringOrderStatus(string name) => Name = name;

    public static RecurringOrderStatus FromName(string name) => name switch
    {
        "Active" => Active,
        "Paused" => Paused,
        "Cancelled" => Cancelled,
        _ => throw new ArgumentException($"Unknown RecurringOrderStatus: '{name}'", nameof(name))
    };

    public void ValidateTransitionTo(RecurringOrderStatus next)
    {
        var valid = (Name, next.Name) switch
        {
            ("Active", "Paused") => true,
            ("Active", "Cancelled") => true,
            ("Paused", "Active") => true,
            ("Paused", "Cancelled") => true,
            _ => false
        };

        if (!valid)
            throw new DomainException(
                $"Cannot transition RecurringOrder from '{Name}' to '{next.Name}'");
    }

    public override string ToString() => Name;
}

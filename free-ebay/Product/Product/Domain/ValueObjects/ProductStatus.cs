namespace Domain.ValueObjects;

public sealed class ProductStatus
{
    public static readonly ProductStatus Draft = new("Draft", 0);
    public static readonly ProductStatus Active = new("Active", 1);
    public static readonly ProductStatus Inactive = new("Inactive", 2);
    public static readonly ProductStatus OutOfStock = new("OutOfStock", 3);
    public static readonly ProductStatus Deleted = new("Deleted", 4);

    public string Name { get; }
    public int Value { get; }

    private readonly HashSet<ProductStatus> _allowedTransitions;
    public IReadOnlyCollection<ProductStatus> AllowedTransitions => _allowedTransitions;

    private ProductStatus(string name, int value)
    {
        Name = name;
        Value = value;
        _allowedTransitions = new HashSet<ProductStatus>();
    }

    static ProductStatus()
    {
        Draft.AllowsTransitionTo(Active, Deleted);
        Active.AllowsTransitionTo(Inactive, OutOfStock, Deleted);
        Inactive.AllowsTransitionTo(Active, Deleted);
        OutOfStock.AllowsTransitionTo(Active, Deleted);
        Deleted.AllowsTransitionTo();
    }

    private void AllowsTransitionTo(params ProductStatus[] targets)
    {
        foreach (var target in targets)
            _allowedTransitions.Add(target);
    }

    public bool CanTransitionTo(ProductStatus target) => _allowedTransitions.Contains(target);

    public void ValidateTransitionTo(ProductStatus target)
    {
        if (!CanTransitionTo(target))
            throw new InvalidOperationException(
                $"Cannot transition from {Name} to {target.Name}. "
                + $"Allowed transitions: {string.Join(", ", _allowedTransitions.Select(t => t.Name))}");
    }

    public static ProductStatus FromValue(int value) => value switch
    {
        0 => Draft,
        1 => Active,
        2 => Inactive,
        3 => OutOfStock,
        4 => Deleted,
        _ => throw new ArgumentException($"Unknown ProductStatus value: {value}", nameof(value))
    };

    public static ProductStatus FromName(string name) => name switch
    {
        "Draft" => Draft,
        "Active" => Active,
        "Inactive" => Inactive,
        "OutOfStock" => OutOfStock,
        "Deleted" => Deleted,
        _ => throw new ArgumentException($"Unknown ProductStatus name: {name}", nameof(name))
    };

    public override string ToString() => Name;
}
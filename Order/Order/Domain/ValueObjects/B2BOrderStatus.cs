namespace Domain.ValueObjects;

public sealed class B2BOrderStatus
{
    public static readonly B2BOrderStatus Draft = new("Draft");
    public static readonly B2BOrderStatus Finalized = new("Finalized");
    public static readonly B2BOrderStatus Cancelled = new("Cancelled");

    public string Name { get; }

    private B2BOrderStatus(string name) => Name = name;

    public static B2BOrderStatus FromName(string name) => name switch
    {
        "Draft"     => Draft,
        "Finalized" => Finalized,
        "Cancelled" => Cancelled,
        _ => throw new ArgumentException($"Unknown B2BOrderStatus: '{name}'", nameof(name))
    };

    public override string ToString() => Name;
}

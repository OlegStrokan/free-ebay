using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed class ListingCondition
{
    public static readonly ListingCondition New = new("New", 0);
    public static readonly ListingCondition Used = new("Used", 1);
    public static readonly ListingCondition Refurbished = new("Refurbished", 2);

    public string Name { get; }
    public int Value { get; }

    private ListingCondition(string name, int value)
    {
        Name = name;
        Value = value;
    }

    public static ListingCondition FromValue(int value) => value switch
    {
        0 => New,
        1 => Used,
        2 => Refurbished,
        _ => throw new InvalidValueException($"Unknown ListingCondition value: {value}")
    };

    public static ListingCondition FromName(string name) => name switch
    {
        "New" => New,
        "Used" => Used,
        "Refurbished" => Refurbished,
        _ => throw new InvalidValueException($"Unknown ListingCondition name: {name}")
    };

    public override bool Equals(object? obj) => obj is ListingCondition other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Name;
}
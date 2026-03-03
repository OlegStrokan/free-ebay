namespace Domain.ValueObjects;

public sealed record RecurringOrderId
{
    public Guid Value { get; }

    private RecurringOrderId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("RecurringOrderId cannot be empty", nameof(value));
        Value = value;
    }

    public static RecurringOrderId From(Guid value) => new(value);
    public static RecurringOrderId CreateUnique() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed class ProductId
{
    public Guid Value { get; init; }

    private ProductId(Guid value)
    {
        if (value == Guid.Empty)
            throw new InvalidValueException("ProductId cannot be empty");
        Value = value;
    }

    public static ProductId From(Guid value) => new ProductId(value);
    
    public static ProductId CreateUnique() => new ProductId(Guid.NewGuid());
    
    public override string ToString() => Value.ToString();
}
namespace Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    // this constructor replaces the manual ValueObject equality logic
    public Money(decimal amount, string currency)
    {
        if (amount < 0) 
            throw new ArgumentException("Money amount cannot be negative", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency cannot be empty", nameof(currency));
        }

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }
    
    // factory methods
    public static Money Create(decimal amount, string currency) => new Money(amount, currency);
    public static Money Default(string currency = "USD") => new(0, currency);
    
    // business logic, @think: should I remove this basic shit?
    public bool IsGreaterThenZero() => Amount > 0;
    public bool IsGreaterThen(Money other) => Amount > other.Amount;
    public bool IsLessThen(Money other) => Amount < other.Amount;
    
    // Arithmetic methods 
    public Money Add(Money other)
    {
        CheckCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        CheckCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }
    
    public Money Multiply(int multiplier) => new Money(Amount * multiplier, Currency);


    private void CheckCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Currencies do not match: {Currency} vs {other.Currency}");
    }
} 
using Domain.Exceptions;

namespace Domain.SearchResults.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0) 
            
            throw new InvalidValueException("Money amount cannot be negative");

        if (string.IsNullOrWhiteSpace(currency))
            throw new InvalidValueException("Currency cannot be empty");

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }
    
    public static Money Create(decimal amount, string currency) => new Money(amount, currency);
    public static Money Default(string currency = "USD") => new(0, currency);
    
    public bool IsGreaterThanZero() => Amount > 0;
    public bool IsGreaterThen(Money other) => Amount > other.Amount;
    public bool IsLessThen(Money other) => Amount < other.Amount;
    
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
            throw new DomainException($"Currencies do not match: {Currency} vs {other.Currency}");
    }
}
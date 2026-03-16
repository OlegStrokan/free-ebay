using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; init; }

    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new InvalidValueException("Money amount cannot be negative");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new InvalidValueException("Currency cannot be empty");
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != 3)
        {
            throw new InvalidValueException("Currency must be a 3-letter code");
        }

        Amount = amount;
        Currency = normalizedCurrency;
    }

    public static Money Create(decimal amount, string currency) => new(amount, currency);

    public bool IsGreaterThanZero() => Amount > 0;

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new DomainException($"Currencies do not match: {Currency} vs {other.Currency}");
        }
    }
}
using Domain.Exceptions;
using Domain.SearchResults.ValueObjects;

namespace Domain.Tests.ValueObjects;

[TestFixture]
public sealed class MoneyTests
{
    [Test]
    public void Constructor_WhenAmountIsNegative_ShouldThrowInvalidValueException()
    {
        Assert.Throws<InvalidValueException>(() => Money.Create(-0.01m, "USD"));
    }

    [Test]
    public void Constructor_WhenCurrencyIsEmpty_ShouldThrowInvalidValueException()
    {
        Assert.Throws<InvalidValueException>(() => Money.Create(10m, ""));
    }

    [Test]
    public void Constructor_ShouldNormalizeCurrencyToUppercase()
    {
        var money = Money.Create(10m, "usd");

        Assert.That(money.Currency, Is.EqualTo("USD"));
    }

    [Test]
    public void Add_WhenCurrencyDiffers_ShouldThrowDomainException()
    {
        var usd = Money.Create(10m, "USD");
        var eur = Money.Create(5m, "EUR");

        Assert.Throws<DomainException>(() => usd.Add(eur));
    }

    [Test]
    public void Subtract_WhenResultWouldBeNegative_ShouldThrowInvalidValueException()
    {
        var a = Money.Create(5m, "USD");
        var b = Money.Create(10m, "USD");

        Assert.Throws<InvalidValueException>(() => a.Subtract(b));
    }

    [Test]
    public void Add_ShouldReturnSummedAmount_WhenCurrencyMatches()
    {
        var a = Money.Create(7.5m, "USD");
        var b = Money.Create(2.5m, "USD");

        var result = a.Add(b);

        Assert.That(result.Amount, Is.EqualTo(10m));
        Assert.That(result.Currency, Is.EqualTo("USD"));
    }
}

using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

[TestFixture]
public class MoneyTests
{
    #region Validation

    [TestCase(-0.01, "USD")]
    [TestCase(-100, "CZK")]
    public void Constructor_WithNegativeAmount_ShouldThrowArgumentException(decimal amount, string currency)
    {
        Assert.Throws<InvalidValueException>(() => Money.Create(amount, currency));
    }

    [TestCase(100, "")]
    [TestCase(100, "   ")]
    [TestCase(100, null)]
    public void Constructor_WithEmptyCurrency_ShouldThrowArgumentException(decimal amount, string? currency)
    {
        Assert.Throws<InvalidValueException>(() => Money.Create(amount, currency!));
    }

    [Test]
    public void Constructor_ShouldNormalizeCurrencyToUpperCase()
    {
        var money = Money.Create(100, "usd");

        Assert.That(money.Currency, Is.EqualTo("USD"));
    }

    [Test]
    public void Constructor_WithZeroAmount_ShouldSucceed()
    {
        var money = Money.Create(0, "USD");

        Assert.That(money.Amount, Is.EqualTo(0));
    }

    #endregion

    #region Arithmetic

    [Test]
    public void Add_WithMatchingCurrencies_ShouldReturnCorrectSum()
    {
        var m1 = Money.Create(100, "USD");
        var m2 = Money.Create(50, "usd");

        var result = m1.Add(m2);

        Assert.That(result.Amount, Is.EqualTo(150));
        Assert.That(result.Currency, Is.EqualTo("USD"));
    }

    [Test]
    public void Add_WithDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        var m1 = Money.Create(100, "USD");
        var m2 = Money.Create(100, "EUR");

        var ex = Assert.Throws<DomainException>(() => m1.Add(m2));

        Assert.That(ex!.Message, Does.Contain("Currencies do not match"));
    }

    [Test]
    public void Subtract_WithMatchingCurrencies_ShouldReturnCorrectDifference()
    {
        var m1 = Money.Create(100, "USD");
        var m2 = Money.Create(40, "usd");

        var result = m1.Subtract(m2);

        Assert.That(result.Amount, Is.EqualTo(60));
        Assert.That(result.Currency, Is.EqualTo("USD"));
    }

    [Test]
    public void Subtract_WithDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        var m1 = Money.Create(100, "USD");
        var m2 = Money.Create(50, "EUR");

        Assert.Throws<DomainException>(() => m1.Subtract(m2));
    }

    [TestCase(10, 2, 20)]
    [TestCase(5.5, 3, 16.5)]
    [TestCase(100, 0, 0)]
    public void Multiply_ShouldReturnCorrectProduct(decimal amount, int multiplier, decimal expected)
    {
        var money = Money.Create(amount, "USD");

        var result = money.Multiply(multiplier);

        Assert.That(result.Amount, Is.EqualTo(expected));
        Assert.That(result.Currency, Is.EqualTo("USD"));
    }

    #endregion

    #region Comparison

    [Test]
    public void IsGreaterThanZero_WhenAmountPositive_ShouldReturnTrue()
    {
        var money = Money.Create(0.01m, "USD");

        Assert.That(money.IsGreaterThanZero(), Is.True);
    }

    [Test]
    public void IsGreaterThanZero_WhenAmountZero_ShouldReturnFalse()
    {
        var money = Money.Create(0, "USD");

        Assert.That(money.IsGreaterThanZero(), Is.False);
    }

    [Test]
    public void IsGreaterThen_WhenAmountIsLarger_ShouldReturnTrue()
    {
        var bigger = Money.Create(200, "USD");
        var smaller = Money.Create(100, "USD");

        Assert.That(bigger.IsGreaterThen(smaller), Is.True);
    }

    [Test]
    public void IsGreaterThen_WhenAmountIsSmaller_ShouldReturnFalse()
    {
        var smaller = Money.Create(50, "USD");
        var bigger = Money.Create(100, "USD");

        Assert.That(smaller.IsGreaterThen(bigger), Is.False);
    }

    [Test]
    public void IsLessThen_WhenAmountIsSmaller_ShouldReturnTrue()
    {
        var smaller = Money.Create(50, "USD");
        var bigger = Money.Create(100, "USD");

        Assert.That(smaller.IsLessThen(bigger), Is.True);
    }

    [Test]
    public void IsLessThen_WhenAmountIsLarger_ShouldReturnFalse()
    {
        var bigger = Money.Create(200, "USD");
        var smaller = Money.Create(100, "USD");

        Assert.That(bigger.IsLessThen(smaller), Is.False);
    }

    #endregion

    #region Equality

    [Test]
    public void Equality_TwoInstancesWithSameValues_ShouldBeEqual()
    {
        var m1 = Money.Create(99.99m, "USD");
        var m2 = Money.Create(99.99m, "usd");

        Assert.That(m1, Is.EqualTo(m2));
    }

    [Test]
    public void Equality_TwoInstancesWithDifferentAmount_ShouldNotBeEqual()
    {
        var m1 = Money.Create(100, "USD");
        var m2 = Money.Create(200, "USD");

        Assert.That(m1, Is.Not.EqualTo(m2));
    }

    [Test]
    public void Default_ShouldReturnZeroAmountWithGivenCurrency()
    {
        var money = Money.Default("EUR");

        Assert.That(money.Amount, Is.EqualTo(0));
        Assert.That(money.Currency, Is.EqualTo("EUR"));
    }

    #endregion
}

using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class MoneyTests
{
    #region Validation

    [Theory]
    [InlineData(-0.01, "USD")]
    [InlineData(-100, "EUR")]
    public void Constructor_WithNegativeAmount_ShouldThrowInvalidValueException(decimal amount, string currency)
    {
        var ex = Assert.Throws<InvalidValueException>(() => Money.Create(amount, currency));

        Assert.Contains("cannot be negative", ex.Message);
    }

    [Theory]
    [InlineData(10, "")]
    [InlineData(10, "   ")]
    public void Constructor_WithEmptyCurrency_ShouldThrowInvalidValueException(decimal amount, string currency)
    {
        Assert.Throws<InvalidValueException>(() => Money.Create(amount, currency));
    }

    [Theory]
    [InlineData(10, "US")]
    [InlineData(10, "UUSD")]
    public void Constructor_WithNon3LetterCurrency_ShouldThrowInvalidValueException(decimal amount, string currency)
    {
        var ex = Assert.Throws<InvalidValueException>(() => Money.Create(amount, currency));

        Assert.Contains("3-letter", ex.Message);
    }

    [Fact]
    public void Constructor_ShouldNormalizeCurrencyToUpperCase()
    {
        var money = Money.Create(10, "usd");

        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Constructor_ShouldTrimCurrencyWhitespace()
    {
        var money = Money.Create(10, " EUR ");

        Assert.Equal("EUR", money.Currency);
    }

    [Fact]
    public void Constructor_WithZeroAmount_ShouldSucceed()
    {
        var money = Money.Create(0, "USD");

        Assert.Equal(0, money.Amount);
    }

    #endregion

    #region IsGreaterThanZero

    [Fact]
    public void IsGreaterThanZero_WhenAmountPositive_ShouldReturnTrue()
    {
        var money = Money.Create(0.01m, "USD");

        Assert.True(money.IsGreaterThanZero());
    }

    [Fact]
    public void IsGreaterThanZero_WhenAmountZero_ShouldReturnFalse()
    {
        var money = Money.Create(0, "USD");

        Assert.False(money.IsGreaterThanZero());
    }

    #endregion

    #region Arithmetic

    [Fact]
    public void Add_WithMatchingCurrencies_ShouldReturnCorrectSum()
    {
        var m1 = Money.Create(100, "USD");
        var m2 = Money.Create(50, "usd");

        var result = m1.Add(m2);

        Assert.Equal(150, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Add_WithDifferentCurrencies_ShouldThrowDomainException()
    {
        var m1 = Money.Create(100, "USD");
        var m2 = Money.Create(100, "EUR");

        var ex = Assert.Throws<DomainException>(() => m1.Add(m2));

        Assert.Contains("Currencies do not match", ex.Message);
    }

    [Fact]
    public void Subtract_WithMatchingCurrencies_ShouldReturnCorrectDifference()
    {
        var m1 = Money.Create(100, "USD");
        var m2 = Money.Create(40, "usd");

        var result = m1.Subtract(m2);

        Assert.Equal(60, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Subtract_WithDifferentCurrencies_ShouldThrowDomainException()
    {
        var m1 = Money.Create(100, "USD");
        var m2 = Money.Create(50, "EUR");

        Assert.Throws<DomainException>(() => m1.Subtract(m2));
    }

    #endregion

    #region Equality

    [Fact]
    public void Equality_TwoInstancesWithSameValues_ShouldBeEqual()
    {
        var m1 = Money.Create(99.99m, "USD");
        var m2 = Money.Create(99.99m, "usd");

        Assert.Equal(m1, m2);
    }

    [Fact]
    public void Equality_TwoInstancesWithDifferentAmount_ShouldNotBeEqual()
    {
        var m1 = Money.Create(100, "USD");
        var m2 = Money.Create(200, "USD");

        Assert.NotEqual(m1, m2);
    }

    [Fact]
    public void Equality_TwoInstancesWithDifferentCurrency_ShouldNotBeEqual()
    {
        var m1 = Money.Create(100, "USD");
        var m2 = Money.Create(100, "EUR");

        Assert.NotEqual(m1, m2);
    }

    #endregion
}

public class FailureReasonTests
{
    [Fact]
    public void Create_WithCodeAndMessage_ShouldSetBothProperties()
    {
        var reason = FailureReason.Create("card_declined", "Your card was declined.");

        Assert.Equal("card_declined", reason.Code);
        Assert.Equal("Your card was declined.", reason.Message);
    }

    [Fact]
    public void Create_WithNullCode_ShouldSetCodeToNull()
    {
        var reason = FailureReason.Create(null, "Some error occurred.");

        Assert.Null(reason.Code);
        Assert.Equal("Some error occurred.", reason.Message);
    }

    [Fact]
    public void Create_WithWhitespaceCode_ShouldNormalizeCodeToNull()
    {
        var reason = FailureReason.Create("   ", "Some error.");

        Assert.Null(reason.Code);
    }

    [Fact]
    public void Create_ShouldTrimMessage()
    {
        var reason = FailureReason.Create(null, "  trimmed message  ");

        Assert.Equal("trimmed message", reason.Message);
    }

    [Fact]
    public void Create_ShouldTrimCode()
    {
        var reason = FailureReason.Create("  code  ", "message");

        Assert.Equal("code", reason.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyMessage_ShouldThrowInvalidValueException(string message)
    {
        var ex = Assert.Throws<InvalidValueException>(() => FailureReason.Create(null, message));

        Assert.Contains("Failure message cannot be empty", ex.Message);
    }

    [Fact]
    public void Create_WithCodeExceeding64Chars_ShouldThrowInvalidValueException()
    {
        var longCode = new string('x', 65);

        var ex = Assert.Throws<InvalidValueException>(() => FailureReason.Create(longCode, "message"));

        Assert.Contains("64", ex.Message);
    }

    [Fact]
    public void Create_WithMessageExceeding1024Chars_ShouldThrowInvalidValueException()
    {
        var longMessage = new string('x', 1025);

        var ex = Assert.Throws<InvalidValueException>(() => FailureReason.Create(null, longMessage));

        Assert.Contains("1024", ex.Message);
    }

    [Fact]
    public void Create_WithExactly64CharCode_ShouldSucceed()
    {
        var exactly64 = new string('x', 64);

        var reason = FailureReason.Create(exactly64, "message");

        Assert.Equal(64, reason.Code!.Length);
    }

    [Fact]
    public void ToString_WithCode_ShouldReturnCodeColonMessage()
    {
        var reason = FailureReason.Create("card_declined", "Your card was declined.");

        Assert.Equal("card_declined: Your card was declined.", reason.ToString());
    }

    [Fact]
    public void ToString_WithoutCode_ShouldReturnMessageOnly()
    {
        var reason = FailureReason.Create(null, "Generic failure.");

        Assert.Equal("Generic failure.", reason.ToString());
    }

    [Fact]
    public void Equality_TwoInstancesWithSameValues_ShouldBeEqual()
    {
        var r1 = FailureReason.Create("err", "failed");
        var r2 = FailureReason.Create("err", "failed");

        Assert.Equal(r1, r2);
    }
}

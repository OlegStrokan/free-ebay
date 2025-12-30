using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class MoneyTests
{
    #region ValidationTests

    [Theory]
    [InlineData(-0.01, "USD")]
    [InlineData(-100, "CZK")]
    public void Constructor_ShouldThrowException_WhenAmountIsNegative(decimal amount, string currency)
    {
        Assert.Throws<ArgumentException>(() => Money.Create(amount, currency));
    }

    [Theory]
    [InlineData(100, "")]
    [InlineData(100, "   ")]
    [InlineData(100, null)]
    public void Constructor_ShouldThrowException_WhenCurrencyIsEmpty(decimal amount, string currency)
    {
        Assert.Throws<ArgumentException>(() => Money.Create(amount, currency));
    }

    [Fact]
    public void Constructor_ShouldNormalizeCurrencyToUpperCase()
    {
        var money = new Money(100, "usd");
        Assert.Equal("USD", money.Currency);
    }
    
    #endregion
    
    #region ArithmeticTests

    [Fact]
    public void Add_ShouldReturnCorrectSum_WhenCurrenciesMatch()
    {
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "usd");

        var sum = money1.Add(money2);
        
        Assert.Equal(200, sum.Amount);
        Assert.Equal("USD", sum.Currency);
    }

    [Fact]
    public void Add_ShouldThrowException_WhenCurrenciesIsNotMatch()
    {
        var money1 = new Money(100, "USDcko");
        var money2 = new Money(100, "CZKcko");

        var exception = Assert.Throws<InvalidOperationException>(() => money1.Add(money2));

        Assert.Contains("Currencies do not match", exception.Message);
    }

    [Fact]
    public void Subtract_ShouldReturnCorrectAmount_WhenCurrenciesMatch()
    {
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "usd");

        var subtractedValue = money1.Subtract(money2);
        
        Assert.Equal(50, subtractedValue.Amount);
        Assert.Equal("USD", subtractedValue.Currency);
    }

    [Fact]
    public void Subtract_ShouldThrowException_WhenCurrenciesIsNotMatch()
    {
        var money1 = new Money(100, "ROLEXky");
        var money2 = new Money(100, "AIcko");

        var exception = Assert.Throws<InvalidOperationException>(() => money1.Subtract(money2));
    }

    [Theory]
    [InlineData(10, 2, 20)]
    [InlineData(5.5, 0, 0)]
    public void Multiply_ShouldReturnCorrectProduct(decimal amount, int multiplier, decimal expected)
    {
        var money = Money.Create(amount, "USD");

        var result = money.Multiply(multiplier);
        
        Assert.Equal(expected, result.Amount);
    }

    #endregion

    #region ComparisonTests


    // test for isGreaterThen and isLessThen
    [Fact]
    public void IsGreaterThen_ShouldReturnTrue_WhenAmountIsGreater()
    {
        var high = Money.Create(100, "USD");
        var low = Money.Create(50, "USD");
        
        Assert.True(high.IsGreaterThen(low));
        Assert.True(low.IsLessThen(high));
    }

    [Fact]
    public void IsGreaterThenZero_ShouldReturnTrue_OnlyForPositiveAmounts()
    {
        var positive = Money.Create(0.01m, "USD");
        var zero = Money.Default("USD");
        
        
        Assert.True(positive.IsGreaterThenZero());
        Assert.False(zero.IsGreaterThenZero());
        
    }
 
    
    #endregion
}
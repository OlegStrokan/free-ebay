using Api.Mappers;
using Domain.ValueObjects;
using Protos.Common;
using Xunit;

namespace Api.Tests;

public class MoneyMapperTests
{
    [Theory]
    [InlineData(100.50, 100, 500_000_000)]
    [InlineData(0.999999999, 0, 999_999_999)]
    [InlineData(-10.25, -10, -250_000_000)]
    [InlineData(0.0, 0, 0)]
    public void ToDecimalValue_ShouldMapCorrectly(decimal input, long expectedUnits, int expectedNanos)
    {
        // Act
        var result = input.ToDecimalValue();

        // Assert
        Assert.Equal(expectedUnits, result.Units);
        Assert.Equal(expectedNanos, result.Nanos);
    }

    [Fact]
    public void ToDecimal_ShouldRestoreFullPrecision()
    {
        // Arrange
        var grpcValue = new DecimalValue { Units = 123, Nanos = 456_789_000 };
        decimal expected = 123.456789m;

        // Act
        var result = grpcValue.ToDecimal();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDomain_ShouldHandleNullByReturningDefault()
    {
        // Arrange
        DecimalValue? grpcValue = null;
        const string currency = "USD";

        // Act
        var result = grpcValue.ToDomain(currency);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0m, result.Amount);
        Assert.Equal(currency, result.Currency);
    }

    [Fact]
    public void ToDecimalValue_FromMoneyObject_ShouldMapAmount()
    {
        // Arrange
        var money = Money.Create(50.25m, "EUR");

        // Act
        var result = money.ToDecimalValue();

        // Assert
        Assert.Equal(50, result.Units);
        Assert.Equal(250_000_000, result.Nanos);
    }

    [Theory]
    [InlineData(10.123456789)]
    [InlineData(123456789.000000001)]
    public void RoundTrip_ShouldPreserveValue(decimal original)
    {
        // Act: Decimal -> Grpc -> Decimal
        var grpc = original.ToDecimalValue();
        var final = grpc.ToDecimal();

        // Assert
        Assert.Equal(original, final);
    }
}
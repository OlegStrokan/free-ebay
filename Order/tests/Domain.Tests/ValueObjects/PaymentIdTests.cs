using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class PaymentIdTests
{
    [Fact]
    public void From_ShouldCreateInstance_WhenValueIsValid()
    {
        var paymentId = PaymentId.From("PAY-123");

        Assert.Equal("PAY-123", paymentId.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void From_ShouldThrow_WhenValueIsNullOrWhitespace(string? value)
    {
        var ex = Assert.Throws<ArgumentException>(() => PaymentId.From(value!));

        Assert.Contains("PaymentId cannot be empty", ex.Message);
    }

    [Fact]
    public void ImplicitConversionToString_ShouldReturnValue()
    {
        var paymentId = PaymentId.From("PAY-456");

        string result = paymentId;

        Assert.Equal("PAY-456", result);
    }

    [Fact]
    public void TwoInstancesWithSameValue_ShouldBeEqual()
    {
        var a = PaymentId.From("PAY-SAME");
        var b = PaymentId.From("PAY-SAME");

        Assert.Equal(a, b);
    }

    [Fact]
    public void TwoInstancesWithDifferentValues_ShouldNotBeEqual()
    {
        var a = PaymentId.From("PAY-A");
        var b = PaymentId.From("PAY-B");

        Assert.NotEqual(a, b);
    }
}

using Api.Mappers;
using Protos.Product;

namespace Api.Tests.Mappers;

[TestFixture]
public class DecimalMapperTests
{
    [TestCase(100.50,100L,500_000_000)]
    [TestCase(0.999999999,0L,999_999_999)]
    [TestCase(0.0,0L,0)]
    [TestCase(1.0,1L,0)]
    public void ToDecimalValue_ShouldMapUnitsAndNanosCorrectly(decimal input, long expectedUnits, int expectedNanos)
    {
        var result = input.ToDecimalValue();

        Assert.That(result.Units, Is.EqualTo(expectedUnits));
        Assert.That(result.Nanos, Is.EqualTo(expectedNanos));
    }

    [Test]
    public void ToDecimal_ShouldRestoreFullPrecision()
    {
        var grpc = new DecimalValue { Units = 123, Nanos = 456_789_000 };

        var result = grpc.ToDecimal();

        Assert.That(result, Is.EqualTo(123.456789m));
    }

    [Test]
    public void ToDecimal_WhenNanosIsZero_ShouldReturnWholeNumber()
    {
        var grpc = new DecimalValue { Units = 42, Nanos = 0 };

        Assert.That(grpc.ToDecimal(), Is.EqualTo(42m));
    }

    [TestCase(10.123456789)]
    [TestCase(0.000000001)]
    [TestCase(999999.50)]
    public void RoundTrip_ShouldPreserveValue(decimal original)
    {
        var grpc  = original.ToDecimalValue();
        var final = grpc.ToDecimal();

        Assert.That(final, Is.EqualTo(original));
    }
}

using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class TrackingIdTests
{
    [Fact]
    public void From_ShouldCreateInstance_WhenValueIsValid()
    {
        var trackingId = TrackingId.From("TRACK-XYZ");

        Assert.Equal("TRACK-XYZ", trackingId.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void From_ShouldThrow_WhenValueIsNullOrWhitespace(string? value)
    {
        var ex = Assert.Throws<ArgumentException>(() => TrackingId.From(value!));

        Assert.Contains("TrackingId cannot be empty", ex.Message);
    }

    [Fact]
    public void ImplicitConversionToString_ShouldReturnValue()
    {
        var trackingId = TrackingId.From("TRACK-001");

        string result = trackingId;

        Assert.Equal("TRACK-001", result);
    }

    [Fact]
    public void TwoInstancesWithSameValue_ShouldBeEqual()
    {
        var a = TrackingId.From("T-SAME");
        var b = TrackingId.From("T-SAME");

        Assert.Equal(a, b);
    }

    [Fact]
    public void TwoInstancesWithDifferentValues_ShouldNotBeEqual()
    {
        var a = TrackingId.From("T-A");
        var b = TrackingId.From("T-B");

        Assert.NotEqual(a, b);
    }
}

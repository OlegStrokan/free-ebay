using Domain.SearchResults.ValueObjects;

namespace Domain.Tests.ValueObjects;

[TestFixture]
public sealed class RelevanceScoreTests
{
    [Test]
    public void Constructor_WhenValueIsNegative_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new RelevanceScore(-0.1));
    }

    [Test]
    public void ImplicitConversionToDouble_ShouldReturnUnderlyingValue()
    {
        var score = new RelevanceScore(2.75);

        double value = score;

        Assert.That(value, Is.EqualTo(2.75));
    }
}

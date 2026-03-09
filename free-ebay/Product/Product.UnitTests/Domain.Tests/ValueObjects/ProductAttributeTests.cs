using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

[TestFixture]
public class ProductAttributeTests
{
    [Test]
    public void Constructor_ShouldNormalizeKeyToLowerCase()
    {
        var attr = new ProductAttribute("Color", "Red");

        Assert.That(attr.Key, Is.EqualTo("color"));
    }

    [Test]
    public void Constructor_ShouldTrimKey()
    {
        var attr = new ProductAttribute("  size  ", "XL");

        Assert.That(attr.Key, Is.EqualTo("size"));
    }

    [Test]
    public void Constructor_ShouldTrimValue()
    {
        var attr = new ProductAttribute("brand", "  Nike  ");

        Assert.That(attr.Value, Is.EqualTo("Nike"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_WithEmptyKey_ShouldThrowArgumentException(string key)
    {
        Assert.Throws<ArgumentException>(() => new ProductAttribute(key, "Red"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_WithEmptyValue_ShouldThrowArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => new ProductAttribute("color", value));
    }

    [Test]
    public void Equality_TwoInstancesWithSameKeyAndValue_ShouldBeEqual()
    {
        var a1 = new ProductAttribute("Color", "Red");
        var a2 = new ProductAttribute("color", "Red");

        Assert.That(a1, Is.EqualTo(a2));
    }

    [Test]
    public void Equality_TwoInstancesWithDifferentValue_ShouldNotBeEqual()
    {
        var a1 = new ProductAttribute("color", "Red");
        var a2 = new ProductAttribute("color", "Blue");

        Assert.That(a1, Is.Not.EqualTo(a2));
    }
}

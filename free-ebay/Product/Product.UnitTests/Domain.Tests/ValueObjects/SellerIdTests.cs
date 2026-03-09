using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

[TestFixture]
public class SellerIdTests
{
    [Test]
    public void CreateUnique_ShouldReturnNonEmptyGuid()
    {
        var id = SellerId.CreateUnique();

        Assert.That(id.Value, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void CreateUnique_CalledTwice_ShouldReturnDifferentValues()
    {
        var id1 = SellerId.CreateUnique();
        var id2 = SellerId.CreateUnique();

        Assert.That(id1.Value, Is.Not.EqualTo(id2.Value));
    }

    [Test]
    public void From_ShouldCreateInstanceWithProvidedGuid()
    {
        var guid = Guid.NewGuid();

        var id = SellerId.From(guid);

        Assert.That(id.Value, Is.EqualTo(guid));
    }

    [Test]
    public void From_WithEmptyGuid_ShouldThrowArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => SellerId.From(Guid.Empty));

        Assert.That(ex!.Message, Does.Contain("SellerId cannot be empty"));
    }

    [Test]
    public void Equality_TwoInstancesWithSameGuid_ShouldBeEqual()
    {
        var guid = Guid.NewGuid();

        var id1 = SellerId.From(guid);
        var id2 = SellerId.From(guid);

        Assert.That(id1, Is.EqualTo(id2));
    }

    [Test]
    public void Equality_TwoInstancesWithDifferentGuid_ShouldNotBeEqual()
    {
        var id1 = SellerId.CreateUnique();
        var id2 = SellerId.CreateUnique();

        Assert.That(id1, Is.Not.EqualTo(id2));
    }

    [Test]
    public void ToString_ShouldReturnGuidString()
    {
        var guid = Guid.NewGuid();
        var id = SellerId.From(guid);

        Assert.That(id.ToString(), Is.EqualTo(guid.ToString()));
    }
}

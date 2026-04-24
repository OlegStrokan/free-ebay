using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

[TestFixture]
public class ProductIdTests
{
    [Test]
    public void CreateUnique_ShouldReturnNonEmptyGuid()
    {
        var id = ProductId.CreateUnique();

        Assert.That(id.Value, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void CreateUnique_CalledTwice_ShouldReturnDifferentValues()
    {
        var id1 = ProductId.CreateUnique();
        var id2 = ProductId.CreateUnique();

        Assert.That(id1.Value, Is.Not.EqualTo(id2.Value));
    }

    [Test]
    public void From_ShouldCreateInstanceWithProvidedGuid()
    {
        var guid = Guid.NewGuid();

        var id = ProductId.From(guid);

        Assert.That(id.Value, Is.EqualTo(guid));
    }

    [Test]
    public void From_WithEmptyGuid_ShouldThrowArgumentException()
    {
        var ex = Assert.Throws<InvalidValueException>(() => ProductId.From(Guid.Empty));

        Assert.That(ex!.Message, Does.Contain("ProductId cannot be empty"));
    }

    [Test]
    public void ToString_ShouldReturnGuidString()
    {
        var guid = Guid.NewGuid();
        var id = ProductId.From(guid);

        Assert.That(id.ToString(), Is.EqualTo(guid.ToString()));
    }
}

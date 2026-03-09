using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

[TestFixture]
public class ProductStatusTests
{
    #region Valid Transitions

    [Test]
    public void Draft_CanTransitionTo_Active() =>
        Assert.That(ProductStatus.Draft.CanTransitionTo(ProductStatus.Active), Is.True);

    [Test]
    public void Draft_CanTransitionTo_Deleted() =>
        Assert.That(ProductStatus.Draft.CanTransitionTo(ProductStatus.Deleted), Is.True);

    [Test]
    public void Active_CanTransitionTo_Inactive() =>
        Assert.That(ProductStatus.Active.CanTransitionTo(ProductStatus.Inactive), Is.True);

    [Test]
    public void Active_CanTransitionTo_OutOfStock() =>
        Assert.That(ProductStatus.Active.CanTransitionTo(ProductStatus.OutOfStock), Is.True);

    [Test]
    public void Active_CanTransitionTo_Deleted() =>
        Assert.That(ProductStatus.Active.CanTransitionTo(ProductStatus.Deleted), Is.True);

    [Test]
    public void Inactive_CanTransitionTo_Active() =>
        Assert.That(ProductStatus.Inactive.CanTransitionTo(ProductStatus.Active), Is.True);

    [Test]
    public void Inactive_CanTransitionTo_Deleted() =>
        Assert.That(ProductStatus.Inactive.CanTransitionTo(ProductStatus.Deleted), Is.True);

    [Test]
    public void OutOfStock_CanTransitionTo_Active() =>
        Assert.That(ProductStatus.OutOfStock.CanTransitionTo(ProductStatus.Active), Is.True);

    [Test]
    public void OutOfStock_CanTransitionTo_Deleted() =>
        Assert.That(ProductStatus.OutOfStock.CanTransitionTo(ProductStatus.Deleted), Is.True);

    #endregion

    #region Invalid Transitions

    [Test]
    public void Draft_CannotTransitionTo_Inactive() =>
        Assert.That(ProductStatus.Draft.CanTransitionTo(ProductStatus.Inactive), Is.False);

    [Test]
    public void Draft_CannotTransitionTo_OutOfStock() =>
        Assert.That(ProductStatus.Draft.CanTransitionTo(ProductStatus.OutOfStock), Is.False);

    [Test]
    public void Deleted_CannotTransitionTo_AnyStatus()
    {
        Assert.That(ProductStatus.Deleted.CanTransitionTo(ProductStatus.Draft), Is.False);
        Assert.That(ProductStatus.Deleted.CanTransitionTo(ProductStatus.Active), Is.False);
        Assert.That(ProductStatus.Deleted.CanTransitionTo(ProductStatus.Inactive), Is.False);
        Assert.That(ProductStatus.Deleted.CanTransitionTo(ProductStatus.OutOfStock), Is.False);
    }

    [Test]
    public void Active_CannotTransitionTo_Draft() =>
        Assert.That(ProductStatus.Active.CanTransitionTo(ProductStatus.Draft), Is.False);

    [Test]
    public void Inactive_CannotTransitionTo_OutOfStock() =>
        Assert.That(ProductStatus.Inactive.CanTransitionTo(ProductStatus.OutOfStock), Is.False);

    #endregion

    #region ValidateTransitionTo

    [Test]
    public void ValidateTransitionTo_WithInvalidTransition_ShouldThrowInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductStatus.Draft.ValidateTransitionTo(ProductStatus.Inactive));

        Assert.That(ex!.Message, Does.Contain("Cannot transition from Draft to Inactive"));
    }

    [Test]
    public void ValidateTransitionTo_FromDeleted_ShouldThrowInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductStatus.Deleted.ValidateTransitionTo(ProductStatus.Active));

        Assert.That(ex!.Message, Does.Contain("Cannot transition from Deleted to Active"));
    }

    [Test]
    public void ValidateTransitionTo_WithValidTransition_ShouldNotThrow()
    {
        Assert.DoesNotThrow(() => ProductStatus.Draft.ValidateTransitionTo(ProductStatus.Active));
    }

    #endregion

    #region FromValue and FromName

    [TestCase(0, "Draft")]
    [TestCase(1, "Active")]
    [TestCase(2, "Inactive")]
    [TestCase(3, "OutOfStock")]
    [TestCase(4, "Deleted")]
    public void FromValue_ShouldReturnCorrectStatus(int value, string expectedName)
    {
        var status = ProductStatus.FromValue(value);

        Assert.That(status.Name, Is.EqualTo(expectedName));
    }

    [Test]
    public void FromValue_WithUnknownValue_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ProductStatus.FromValue(99));
    }

    [TestCase("Draft")]
    [TestCase("Active")]
    [TestCase("Inactive")]
    [TestCase("OutOfStock")]
    [TestCase("Deleted")]
    public void FromName_ShouldReturnCorrectStatus(string name)
    {
        var status = ProductStatus.FromName(name);

        Assert.That(status.Name, Is.EqualTo(name));
    }

    [Test]
    public void FromName_WithUnknownName_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ProductStatus.FromName("Unknown"));
    }

    #endregion

    #region Properties

    [Test]
    public void ToString_ShouldReturnStatusName()
    {
        Assert.That(ProductStatus.Active.ToString(), Is.EqualTo("Active"));
        Assert.That(ProductStatus.Draft.ToString(), Is.EqualTo("Draft"));
        Assert.That(ProductStatus.Deleted.ToString(), Is.EqualTo("Deleted"));
    }

    [Test]
    public void AllowedTransitions_ShouldBeReadOnly()
    {
        Assert.That(ProductStatus.Draft.AllowedTransitions, Is.InstanceOf<IReadOnlyCollection<ProductStatus>>());
    }

    #endregion
}

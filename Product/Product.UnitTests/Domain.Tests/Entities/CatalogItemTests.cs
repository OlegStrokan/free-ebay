using Domain.Entities;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Tests.Entities;

[TestFixture]
public sealed class CatalogItemTests
{
    [Test]
    public void Create_ShouldSetCanonicalProductFields()
    {
        var categoryId = CategoryId.CreateUnique();
        var item = CatalogItem.Create(
            "Sony A7 IV",
            "Full-frame mirrorless camera",
            categoryId,
            "4548736133730",
            [new ProductAttribute("sensor", "33MP")],
            ["https://example.com/a7iv.jpg"]);

        Assert.That(item.Name, Is.EqualTo("Sony A7 IV"));
        Assert.That(item.CategoryId, Is.EqualTo(categoryId));
        Assert.That(item.Gtin, Is.EqualTo("4548736133730"));
        Assert.That(item.Attributes, Has.Count.EqualTo(1));
        Assert.That(item.DomainEvents.OfType<CatalogItemCreatedEvent>(), Has.Exactly(1).Items);
    }

    [Test]
    public void Update_ShouldRaiseCatalogItemUpdatedEvent()
    {
        var item = CatalogItem.Create("Name", "Desc", CategoryId.CreateUnique(), null, [], []);
        item.ClearDomainEvents();
        var categoryId = CategoryId.CreateUnique();

        item.Update("Sony A7 IV", "Updated", categoryId, " 1234567890123 ", [], []);

        Assert.That(item.Name, Is.EqualTo("Sony A7 IV"));
        Assert.That(item.Gtin, Is.EqualTo("1234567890123"));
        Assert.That(item.DomainEvents.OfType<CatalogItemUpdatedEvent>().Single().CatalogItemId, Is.EqualTo(item.Id));
    }

    [Test]
    public void Create_WithEmptyName_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() =>
            CatalogItem.Create(" ", "Desc", CategoryId.CreateUnique(), null, [], []));
    }
}
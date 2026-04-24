using Domain.Entities;
using Domain.Exceptions;
using Domain.SearchResults.ValueObjects;

namespace Domain.Tests.Entities;

[TestFixture]
public sealed class ProductSearchResultTests
{
    [Test]
    public void Create_WhenArgumentsAreValid_ShouldReturnEntity()
    {
        var productId = Guid.NewGuid();

        var result = ProductSearchResult.Create(
            productId,
            "Ultra Phone",
            "Phones",
            Money.Create(999.99m, "USD"),
            new RelevanceScore(1.5),
            ["https://img.test/phone-1.png"]);

        Assert.That(result.ProductId, Is.EqualTo(productId));
        Assert.That(result.Name, Is.EqualTo("Ultra Phone"));
        Assert.That(result.Category, Is.EqualTo("Phones"));
        Assert.That(result.Price.Amount, Is.EqualTo(999.99m));
        Assert.That(result.RelevanceScore, Is.EqualTo(1.5));
        Assert.That(result.ImageUrls, Has.Count.EqualTo(1));
    }

    [Test]
    public void Create_WhenProductIdIsEmpty_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => ProductSearchResult.Create(
            Guid.Empty,
            "Item",
            "General",
            Money.Create(1m, "USD"),
            new RelevanceScore(1),
            []));

        Assert.That(ex!.Message, Does.Contain("productId"));
    }

    [Test]
    public void Create_WhenNameIsWhitespace_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => ProductSearchResult.Create(
            Guid.NewGuid(),
            " ",
            "General",
            Money.Create(1m, "USD"),
            new RelevanceScore(1),
            []));

        Assert.That(ex!.Message, Does.Contain("name"));
    }

    [Test]
    public void Create_WhenCategoryIsWhitespace_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => ProductSearchResult.Create(
            Guid.NewGuid(),
            "Name",
            " ",
            Money.Create(1m, "USD"),
            new RelevanceScore(1),
            []));

        Assert.That(ex!.Message, Does.Contain("category"));
    }

    [Test]
    public void Create_WhenPriceIsNull_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => ProductSearchResult.Create(
            Guid.NewGuid(),
            "Name",
            "Category",
            null!,
            new RelevanceScore(1),
            []));

        Assert.That(ex!.Message, Does.Contain("price"));
    }

    [Test]
    public void Create_WhenImageUrlsNull_ShouldDefaultToEmptyList()
    {
        var result = ProductSearchResult.Create(
            Guid.NewGuid(),
            "Name",
            "Category",
            Money.Create(10m, "USD"),
            new RelevanceScore(1),
            null!);

        Assert.That(result.ImageUrls, Is.Empty);
    }
}

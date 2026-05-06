using System.Text.Json;
using Application.Consumers;
using Application.Models;
using Application.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.Consumers;

[TestFixture]
public class ProductCreatedConsumerTests
{
    private IElasticsearchIndexer _indexer;
    private ILogger<ProductCreatedConsumer> _logger;
    private ProductCreatedConsumer _sut;

    [SetUp]
    public void SetUp()
    {
        _indexer = Substitute.For<IElasticsearchIndexer>();
        _logger = Substitute.For<ILogger<ProductCreatedConsumer>>();
        _sut = new ProductCreatedConsumer(_indexer, _logger);
    }

    [Test]
    public void EventType_ShouldBe_ProductCreatedEvent()
    {
        Assert.That(_sut.EventType, Is.EqualTo("ProductCreatedEvent"));
    }

    [Test]
    public async Task ConsumeAsync_ShouldCallUpsertAsync_WithCorrectProductId()
    {
        var productId = Guid.NewGuid();
        var payload = BuildPayload(productId, Guid.NewGuid(), Guid.NewGuid(),
            "Widget", "A widget", 99.99m, "USD", 5, [], [], DateTime.UtcNow);

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        await _indexer.Received(1).UpsertAsync(
            Arg.Is<ProductSearchDocument>(d => d.Id == productId.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConsumeAsync_ShouldMapAllFieldsToDocument()
    {
        var productId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var createdAt = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var imageUrls = new[] { "https://example.com/img1.jpg" };
        var attributes = new[] { new { Key = "color", Value = "red" } };

        var payload = BuildPayload(productId, sellerId, categoryId,
            "Keyboard", "Mech keyboard", 149.99m, "USD", 10, attributes, imageUrls, createdAt);

        ProductSearchDocument? captured = null;
        _indexer.When(x => x.UpsertAsync(Arg.Any<ProductSearchDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<ProductSearchDocument>(0));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Id, Is.EqualTo(productId.ToString()));
        Assert.That(captured.Name, Is.EqualTo("Keyboard"));
        Assert.That(captured.Description, Is.EqualTo("Mech keyboard"));
        Assert.That(captured.CategoryId, Is.EqualTo(categoryId.ToString()));
        Assert.That(captured.Price, Is.EqualTo(149.99m));
        Assert.That(captured.Currency, Is.EqualTo("USD"));
        Assert.That(captured.Stock, Is.EqualTo(10));
        Assert.That(captured.SellerId, Is.EqualTo(sellerId.ToString()));
        Assert.That(captured.CreatedAt, Is.EqualTo(createdAt));
        Assert.That(captured.ImageUrls, Has.Count.EqualTo(1));
        Assert.That(captured.ProductType, Is.EqualTo("product"));
    }

    [Test]
    public async Task ConsumeAsync_ShouldAlwaysSetStatusToDraft()
    {
        var payload = BuildPayload(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Name", "Desc", 10m, "USD", 5, [], [], DateTime.UtcNow);

        ProductSearchDocument? captured = null;
        _indexer.When(x => x.UpsertAsync(Arg.Any<ProductSearchDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<ProductSearchDocument>(0));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured!.Status, Is.EqualTo("Draft"));
    }

    [Test]
    public async Task ConsumeAsync_ShouldMapAttributesToDictionary()
    {
        var attributes = new[]
        {
            new { Key = "color", Value = "blue" },
            new { Key = "size", Value = "XL"   },
        };
        var payload = BuildPayload(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Shirt", "Desc", 29.99m, "USD", 3, attributes, [], DateTime.UtcNow);

        ProductSearchDocument? captured = null;
        _indexer.When(x => x.UpsertAsync(Arg.Any<ProductSearchDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<ProductSearchDocument>(0));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured!.Attributes, Contains.Key("color"));
        Assert.That(captured.Attributes["color"], Is.EqualTo("blue"));
        Assert.That(captured.Attributes["size"],  Is.EqualTo("XL"));
    }

    [Test]
    public async Task ConsumeAsync_WhenPayloadIsNull_ShouldNotCallIndexer()
    {
        var nullPayload = JsonDocument.Parse("null").RootElement;

        await _sut.ConsumeAsync(nullPayload, CancellationToken.None);

        await _indexer.DidNotReceiveWithAnyArgs().UpsertAsync(null!, default);
    }

    private static JsonElement BuildPayload(
        Guid productId, Guid sellerId, Guid categoryId,
        string name, string description,
        decimal price, string currency, int stock,
        object[] attributes, string[] imageUrls,
        DateTime createdAt)
    {
        var json = JsonSerializer.Serialize(new
        {
            ProductId = new { Value = productId },
            SellerId = new { Value = sellerId },
            Name = name,
            Description = description,
            CategoryId = new { Value = categoryId },
            Price = new { Amount = price, Currency = currency },
            InitialStock = stock,
            Attributes = attributes,
            ImageUrls = imageUrls,
            CreatedAt = createdAt,
        });
        return JsonDocument.Parse(json).RootElement;
    }
}

[TestFixture]
public class ProductUpdatedConsumerTests
{
    private IElasticsearchIndexer _indexer;
    private ILogger<ProductUpdatedConsumer> _logger;
    private ProductUpdatedConsumer _sut;

    [SetUp]
    public void SetUp()
    {
        _indexer = Substitute.For<IElasticsearchIndexer>();
        _logger = Substitute.For<ILogger<ProductUpdatedConsumer>>();
        _sut = new ProductUpdatedConsumer(_indexer, _logger);
    }

    [Test]
    public void EventType_ShouldBe_ProductUpdatedEvent()
    {
        Assert.That(_sut.EventType, Is.EqualTo("ProductUpdatedEvent"));
    }

    [Test]
    public async Task ConsumeAsync_ShouldCallUpdateFieldsAsync_WithCorrectProductId()
    {
        var productId = Guid.NewGuid();
        var payload   = BuildPayload(productId, Guid.NewGuid(), "Name", "Desc", 99m, "USD", [], [], DateTime.UtcNow);

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        await _indexer.Received(1).UpdateFieldsAsync(
            productId.ToString(),
            Arg.Any<ProductFieldsUpdateDocument>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConsumeAsync_ShouldMapAllFieldsToUpdateDocument()
    {
        var productId  = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var updatedAt = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var payload = BuildPayload(productId, categoryId, "New Name", "New Desc", 199.99m, "EUR", [], [], updatedAt);

        ProductFieldsUpdateDocument? captured = null;
        _indexer.When(x => x.UpdateFieldsAsync(Arg.Any<string>(), Arg.Any<ProductFieldsUpdateDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<ProductFieldsUpdateDocument>(1));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Name, Is.EqualTo("New Name"));
        Assert.That(captured.Description, Is.EqualTo("New Desc"));
        Assert.That(captured.CategoryId, Is.EqualTo(categoryId.ToString()));
        Assert.That(captured.Price, Is.EqualTo(199.99m));
        Assert.That(captured.Currency, Is.EqualTo("EUR"));
        Assert.That(captured.UpdatedAt, Is.EqualTo(updatedAt));
    }

    [Test]
    public async Task ConsumeAsync_ShouldMapAttributesToDictionary()
    {
        var attributes = new[] { new { Key = "layout", Value = "tenkeyless" } };
        var payload = BuildPayload(Guid.NewGuid(), Guid.NewGuid(), "Name", "Desc", 10m, "USD", attributes, [], DateTime.UtcNow);

        ProductFieldsUpdateDocument? captured = null;
        _indexer.When(x => x.UpdateFieldsAsync(Arg.Any<string>(), Arg.Any<ProductFieldsUpdateDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<ProductFieldsUpdateDocument>(1));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured!.Attributes, Contains.Key("layout"));
        Assert.That(captured.Attributes["layout"], Is.EqualTo("tenkeyless"));
    }

    [Test]
    public async Task ConsumeAsync_WhenPayloadIsNull_ShouldNotCallIndexer()
    {
        var nullPayload = JsonDocument.Parse("null").RootElement;

        await _sut.ConsumeAsync(nullPayload, CancellationToken.None);

        await _indexer.DidNotReceiveWithAnyArgs()
            .UpdateFieldsAsync(default!, default!, default);
    }

    private static JsonElement BuildPayload(
        Guid productId, Guid categoryId,
        string name, string description,
        decimal price, string currency,
        object[] attributes, string[] imageUrls,
        DateTime updatedAt)
    {
        var json = JsonSerializer.Serialize(new
        {
            ProductId = new { Value = productId },
            Name = name,
            Description = description,
            CategoryId = new { Value = categoryId },
            Price = new { Amount = price, Currency = currency },
            Attributes = attributes,
            ImageUrls = imageUrls,
            UpdatedAt = updatedAt,
        });
        return JsonDocument.Parse(json).RootElement;
    }
}

[TestFixture]
public class ProductStockUpdatedConsumerTests
{
    private IElasticsearchIndexer _indexer;
    private ILogger<ProductStockUpdatedConsumer> _logger;
    private ProductStockUpdatedConsumer _sut;

    [SetUp]
    public void SetUp()
    {
        _indexer = Substitute.For<IElasticsearchIndexer>();
        _logger = Substitute.For<ILogger<ProductStockUpdatedConsumer>>();
        _sut = new ProductStockUpdatedConsumer(_indexer, _logger);
    }

    [Test]
    public void EventType_ShouldBe_ProductStockUpdatedEvent()
    {
        Assert.That(_sut.EventType, Is.EqualTo("ProductStockUpdatedEvent"));
    }

    [Test]
    public async Task ConsumeAsync_ShouldCallUpdateStockAsync_WithCorrectProductId()
    {
        var productId = Guid.NewGuid();
        var payload = BuildPayload(productId, 10, 25);

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        await _indexer.Received(1).UpdateStockAsync(
            productId.ToString(),
            Arg.Any<StockUpdateDocument>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConsumeAsync_ShouldUseNewQuantity_NotPreviousQuantity()
    {
        var payload = BuildPayload(Guid.NewGuid(), previousQuantity: 50, newQuantity: 0);

        StockUpdateDocument? captured = null;
        _indexer.When(x => x.UpdateStockAsync(Arg.Any<string>(), Arg.Any<StockUpdateDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<StockUpdateDocument>(1));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured!.Stock, Is.EqualTo(0));
    }

    [Test]
    public async Task ConsumeAsync_ShouldSetNewQuantityOnDocument()
    {
        var payload = BuildPayload(Guid.NewGuid(), previousQuantity: 5, newQuantity: 42);

        StockUpdateDocument? captured = null;
        _indexer.When(x => x.UpdateStockAsync(Arg.Any<string>(), Arg.Any<StockUpdateDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<StockUpdateDocument>(1));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured!.Stock, Is.EqualTo(42));
    }

    [Test]
    public async Task ConsumeAsync_WhenPayloadIsNull_ShouldNotCallIndexer()
    {
        var nullPayload = JsonDocument.Parse("null").RootElement;

        await _sut.ConsumeAsync(nullPayload, CancellationToken.None);

        await _indexer.DidNotReceiveWithAnyArgs()
            .UpdateStockAsync(default!, default!, default);
    }

    private static JsonElement BuildPayload(Guid productId, int previousQuantity, int newQuantity)
    {
        var json = JsonSerializer.Serialize(new
        {
            ProductId = new { Value = productId },
            PreviousQuantity = previousQuantity,
            NewQuantity = newQuantity,
        });
        return JsonDocument.Parse(json).RootElement;
    }
}

[TestFixture]
public class ProductStatusChangedConsumerTests
{
    private IElasticsearchIndexer _indexer;
    private ILogger<ProductStatusChangedConsumer> _logger;
    private ProductStatusChangedConsumer _sut;

    [SetUp]
    public void SetUp()
    {
        _indexer = Substitute.For<IElasticsearchIndexer>();
        _logger = Substitute.For<ILogger<ProductStatusChangedConsumer>>();
        _sut = new ProductStatusChangedConsumer(_indexer, _logger);
    }

    [Test]
    public void EventType_ShouldBe_ProductStatusChangedEvent()
    {
        Assert.That(_sut.EventType, Is.EqualTo("ProductStatusChangedEvent"));
    }

    [Test]
    public async Task ConsumeAsync_ShouldCallUpdateStatusAsync_WithCorrectProductId()
    {
        var productId = Guid.NewGuid();
        var payload   = BuildPayload(productId, "Draft", "Active");

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        await _indexer.Received(1).UpdateStatusAsync(
            productId.ToString(),
            Arg.Any<StatusUpdateDocument>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConsumeAsync_ShouldUseNewStatus_NotPreviousStatus()
    {
        var payload = BuildPayload(Guid.NewGuid(), "Active", "Inactive");

        StatusUpdateDocument? captured = null;
        _indexer.When(x => x.UpdateStatusAsync(Arg.Any<string>(), Arg.Any<StatusUpdateDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<StatusUpdateDocument>(1));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured!.Status, Is.EqualTo("Inactive"));
    }

    [TestCase("Draft", "Active")]
    [TestCase("Active", "Inactive")]
    [TestCase("Active", "OutOfStock")]
    [TestCase("Active", "Deleted")]
    [TestCase("OutOfStock","Active" )]
    public async Task ConsumeAsync_ShouldPersistNewStatus_ForVariousTransitions(
        string previousStatus, string newStatus)
    {
        var payload = BuildPayload(Guid.NewGuid(), previousStatus, newStatus);

        StatusUpdateDocument? captured = null;
        _indexer.When(x => x.UpdateStatusAsync(Arg.Any<string>(), Arg.Any<StatusUpdateDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<StatusUpdateDocument>(1));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured!.Status, Is.EqualTo(newStatus));
    }

    [Test]
    public async Task ConsumeAsync_WhenPayloadIsNull_ShouldNotCallIndexer()
    {
        var nullPayload = JsonDocument.Parse("null").RootElement;

        await _sut.ConsumeAsync(nullPayload, CancellationToken.None);

        await _indexer.DidNotReceiveWithAnyArgs()
            .UpdateStatusAsync(default!, default!, default);
    }

    private static JsonElement BuildPayload(Guid productId, string previousStatus, string newStatus)
    {
        var json = JsonSerializer.Serialize(new
        {
            ProductId = new { Value = productId },
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
        });
        return JsonDocument.Parse(json).RootElement;
    }
}

[TestFixture]
public class ProductDeletedConsumerTests
{
    private IElasticsearchIndexer _indexer;
    private ILogger<ProductDeletedConsumer> _logger;
    private ProductDeletedConsumer _sut;

    [SetUp]
    public void SetUp()
    {
        _indexer = Substitute.For<IElasticsearchIndexer>();
        _logger = Substitute.For<ILogger<ProductDeletedConsumer>>();
        _sut = new ProductDeletedConsumer(_indexer, _logger);
    }

    [Test]
    public void EventType_ShouldBe_ProductDeletedEvent()
    {
        Assert.That(_sut.EventType, Is.EqualTo("ProductDeletedEvent"));
    }

    [Test]
    public async Task ConsumeAsync_ShouldCallDeleteAsync_WithCorrectProductId()
    {
        var productId = Guid.NewGuid();
        var payload   = BuildPayload(productId);

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        await _indexer.Received(1).DeleteAsync(productId.ToString(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConsumeAsync_ShouldNotCallAnyOtherIndexerMethod()
    {
        var payload = BuildPayload(Guid.NewGuid());

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        await _indexer.DidNotReceiveWithAnyArgs().UpsertAsync(null!, default);
        await _indexer.DidNotReceiveWithAnyArgs().UpdateFieldsAsync(default!, default!, default);
        await _indexer.DidNotReceiveWithAnyArgs().UpdateStockAsync(default!, default!, default);
        await _indexer.DidNotReceiveWithAnyArgs().UpdateStatusAsync(default!, default!, default);
    }

    [Test]
    public async Task ConsumeAsync_WhenPayloadIsNull_ShouldNotCallIndexer()
    {
        var nullPayload = JsonDocument.Parse("null").RootElement;

        await _sut.ConsumeAsync(nullPayload, CancellationToken.None);

        await _indexer.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }

    private static JsonElement BuildPayload(Guid productId)
    {
        var json = JsonSerializer.Serialize(new
        {
            ProductId = new { Value = productId },
        });
        return JsonDocument.Parse(json).RootElement;
    }
}

[TestFixture]
public class CatalogItemCreatedConsumerTests
{
    private IElasticsearchIndexer _indexer;
    private ILogger<CatalogItemCreatedConsumer> _logger;
    private CatalogItemCreatedConsumer _sut;

    [SetUp]
    public void SetUp()
    {
        _indexer = Substitute.For<IElasticsearchIndexer>();
        _logger = Substitute.For<ILogger<CatalogItemCreatedConsumer>>();
        _sut = new CatalogItemCreatedConsumer(_indexer, _logger);
    }

    [Test]
    public void EventType_ShouldBe_CatalogItemCreatedEvent()
    {
        Assert.That(_sut.EventType, Is.EqualTo("CatalogItemCreatedEvent"));
    }

    [Test]
    public async Task ConsumeAsync_ShouldCallUpsertAsync_WithCorrectCatalogItemId()
    {
        var catalogItemId = Guid.NewGuid();
        var payload = BuildPayload(catalogItemId, Guid.NewGuid(), "iPhone 16", "Apple phone", [], [], DateTime.UtcNow);

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        await _indexer.Received(1).UpsertAsync(
            Arg.Is<ProductSearchDocument>(d => d.Id == catalogItemId.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConsumeAsync_ShouldSetProductType_ToCatalogItem()
    {
        var payload = BuildPayload(Guid.NewGuid(), Guid.NewGuid(), "Name", "Desc", [], [], DateTime.UtcNow);

        ProductSearchDocument? captured = null;
        _indexer.When(x => x.UpsertAsync(Arg.Any<ProductSearchDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<ProductSearchDocument>(0));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured!.ProductType, Is.EqualTo("catalog_item"));
    }

    [Test]
    public async Task ConsumeAsync_ShouldSetInitialStatus_ToOutOfStock()
    {
        var payload = BuildPayload(Guid.NewGuid(), Guid.NewGuid(), "Name", "Desc", [], [], DateTime.UtcNow);

        ProductSearchDocument? captured = null;
        _indexer.When(x => x.UpsertAsync(Arg.Any<ProductSearchDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<ProductSearchDocument>(0));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured!.Status, Is.EqualTo("OutOfStock"));
    }

    [Test]
    public async Task ConsumeAsync_ShouldSetInitialMinPrice_ToNull()
    {
        var payload = BuildPayload(Guid.NewGuid(), Guid.NewGuid(), "Name", "Desc", [], [], DateTime.UtcNow);

        ProductSearchDocument? captured = null;
        _indexer.When(x => x.UpsertAsync(Arg.Any<ProductSearchDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<ProductSearchDocument>(0));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured!.MinPrice, Is.Null);
        Assert.That(captured.MinPriceCurrency, Is.Null);
    }

    [Test]
    public async Task ConsumeAsync_ShouldSetHasActiveListings_ToFalse()
    {
        var payload = BuildPayload(Guid.NewGuid(), Guid.NewGuid(), "Name", "Desc", [], [], DateTime.UtcNow);

        ProductSearchDocument? captured = null;
        _indexer.When(x => x.UpsertAsync(Arg.Any<ProductSearchDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<ProductSearchDocument>(0));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured!.HasActiveListings, Is.False);
        Assert.That(captured.SellerCount, Is.EqualTo(0));
    }

    [Test]
    public async Task ConsumeAsync_WhenPayloadIsNull_ShouldNotCallIndexer()
    {
        var nullPayload = JsonDocument.Parse("null").RootElement;

        await _sut.ConsumeAsync(nullPayload, CancellationToken.None);

        await _indexer.DidNotReceiveWithAnyArgs().UpsertAsync(null!, default);
    }

    private static JsonElement BuildPayload(
        Guid catalogItemId, Guid categoryId,
        string name, string description,
        object[] attributes, string[] imageUrls,
        DateTime createdAt)
    {
        var json = JsonSerializer.Serialize(new
        {
            CatalogItemId = new { Value = catalogItemId },
            CategoryId = new { Value = categoryId },
            Name = name,
            Description = description,
            Attributes = attributes,
            ImageUrls = imageUrls,
            CreatedAt = createdAt,
        });
        return JsonDocument.Parse(json).RootElement;
    }
}

[TestFixture]
public class CatalogItemUpdatedConsumerTests
{
    private IElasticsearchIndexer _indexer;
    private ILogger<CatalogItemUpdatedConsumer> _logger;
    private CatalogItemUpdatedConsumer _sut;

    [SetUp]
    public void SetUp()
    {
        _indexer = Substitute.For<IElasticsearchIndexer>();
        _logger = Substitute.For<ILogger<CatalogItemUpdatedConsumer>>();
        _sut = new CatalogItemUpdatedConsumer(_indexer, _logger);
    }

    [Test]
    public void EventType_ShouldBe_CatalogItemUpdatedEvent()
    {
        Assert.That(_sut.EventType, Is.EqualTo("CatalogItemUpdatedEvent"));
    }

    [Test]
    public async Task ConsumeAsync_ShouldCallUpdateCatalogItemFieldsAsync_WithCorrectId()
    {
        var catalogItemId = Guid.NewGuid();
        var payload = BuildPayload(catalogItemId, Guid.NewGuid(), "Name", "Desc", [], [], DateTime.UtcNow);

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        await _indexer.Received(1).UpdateCatalogItemFieldsAsync(
            catalogItemId.ToString(),
            Arg.Any<CatalogItemFieldsUpdateDocument>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConsumeAsync_ShouldMapAllUpdatedFields()
    {
        var categoryId = Guid.NewGuid();
        var updatedAt = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var attributes = new[] { new { Key = "color", Value = "silver" } };
        var imageUrls = new[] { "https://img.test/iphone.png" };
        var payload = BuildPayload(Guid.NewGuid(), categoryId, "iPhone 16 Pro", "Updated desc", attributes, imageUrls, updatedAt);

        CatalogItemFieldsUpdateDocument? captured = null;
        _indexer.When(x => x.UpdateCatalogItemFieldsAsync(Arg.Any<string>(), Arg.Any<CatalogItemFieldsUpdateDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<CatalogItemFieldsUpdateDocument>(1));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured!.Name, Is.EqualTo("iPhone 16 Pro"));
        Assert.That(captured.Description, Is.EqualTo("Updated desc"));
        Assert.That(captured.CategoryId, Is.EqualTo(categoryId.ToString()));
        Assert.That(captured.Attributes["color"], Is.EqualTo("silver"));
        Assert.That(captured.UpdatedAt, Is.EqualTo(updatedAt));
    }

    [Test]
    public async Task ConsumeAsync_WhenPayloadIsNull_ShouldNotCallIndexer()
    {
        var nullPayload = JsonDocument.Parse("null").RootElement;

        await _sut.ConsumeAsync(nullPayload, CancellationToken.None);

        await _indexer.DidNotReceiveWithAnyArgs().UpdateCatalogItemFieldsAsync(default!, default!, default);
    }

    private static JsonElement BuildPayload(
        Guid catalogItemId, Guid categoryId,
        string name, string description,
        object[] attributes, string[] imageUrls,
        DateTime updatedAt)
    {
        var json = JsonSerializer.Serialize(new
        {
            CatalogItemId = new { Value = catalogItemId },
            CategoryId = new { Value = categoryId },
            Name = name,
            Description = description,
            Attributes = attributes,
            ImageUrls = imageUrls,
            UpdatedAt = updatedAt,
        });
        return JsonDocument.Parse(json).RootElement;
    }
}

[TestFixture]
public class CatalogItemListingSummaryUpdatedConsumerTests
{
    private IElasticsearchIndexer _indexer;
    private ILogger<CatalogItemListingSummaryUpdatedConsumer> _logger;
    private CatalogItemListingSummaryUpdatedConsumer _sut;

    [SetUp]
    public void SetUp()
    {
        _indexer = Substitute.For<IElasticsearchIndexer>();
        _logger = Substitute.For<ILogger<CatalogItemListingSummaryUpdatedConsumer>>();
        _sut = new CatalogItemListingSummaryUpdatedConsumer(_indexer, _logger);
    }

    [Test]
    public void EventType_ShouldBe_CatalogItemListingSummaryUpdatedEvent()
    {
        Assert.That(_sut.EventType, Is.EqualTo("CatalogItemListingSummaryUpdatedEvent"));
    }

    [Test]
    public async Task ConsumeAsync_ShouldCallUpdateListingSummaryAsync_WithCorrectId()
    {
        var catalogItemId = Guid.NewGuid();
        var payload = BuildPayload(catalogItemId, 899.99m, "USD", 3, true, "New", 47, DateTime.UtcNow);

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        await _indexer.Received(1).UpdateListingSummaryAsync(
            catalogItemId.ToString(),
            Arg.Any<ListingSummaryUpdateDocument>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConsumeAsync_ShouldMapAllSummaryFields()
    {
        var updatedAt = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var payload = BuildPayload(Guid.NewGuid(), 749.99m, "EUR", 5, true, "Refurbished", 120, updatedAt);

        ListingSummaryUpdateDocument? captured = null;
        _indexer.When(x => x.UpdateListingSummaryAsync(Arg.Any<string>(), Arg.Any<ListingSummaryUpdateDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<ListingSummaryUpdateDocument>(1));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured!.MinPrice, Is.EqualTo(749.99m));
        Assert.That(captured.MinPriceCurrency, Is.EqualTo("EUR"));
        Assert.That(captured.SellerCount, Is.EqualTo(5));
        Assert.That(captured.HasActiveListings, Is.True);
        Assert.That(captured.BestCondition, Is.EqualTo("Refurbished"));
        Assert.That(captured.TotalStock, Is.EqualTo(120));
        Assert.That(captured.UpdatedAt, Is.EqualTo(updatedAt));
    }

    [Test]
    public async Task ConsumeAsync_WhenNoActiveListings_ShouldPassHasActiveListingsFalse()
    {
        var payload = BuildPayload(Guid.NewGuid(), 0m, "USD", 0, false, null, 0, DateTime.UtcNow);

        ListingSummaryUpdateDocument? captured = null;
        _indexer.When(x => x.UpdateListingSummaryAsync(Arg.Any<string>(), Arg.Any<ListingSummaryUpdateDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<ListingSummaryUpdateDocument>(1));

        await _sut.ConsumeAsync(payload, CancellationToken.None);

        Assert.That(captured!.HasActiveListings, Is.False);
        Assert.That(captured.SellerCount, Is.EqualTo(0));
        Assert.That(captured.BestCondition, Is.Null);
    }

    [Test]
    public async Task ConsumeAsync_WhenPayloadIsNull_ShouldNotCallIndexer()
    {
        var nullPayload = JsonDocument.Parse("null").RootElement;

        await _sut.ConsumeAsync(nullPayload, CancellationToken.None);

        await _indexer.DidNotReceiveWithAnyArgs().UpdateListingSummaryAsync(default!, default!, default);
    }

    private static JsonElement BuildPayload(
        Guid catalogItemId, decimal minPrice, string minPriceCurrency,
        int sellerCount, bool hasActiveListings, string? bestCondition,
        int totalStock, DateTime updatedAt)
    {
        var json = JsonSerializer.Serialize(new
        {
            CatalogItemId = new { Value = catalogItemId },
            MinPrice = minPrice,
            MinPriceCurrency = minPriceCurrency,
            SellerCount = sellerCount,
            HasActiveListings = hasActiveListings,
            BestCondition = bestCondition,
            TotalStock = totalStock,
            UpdatedAt = updatedAt,
        });
        return JsonDocument.Parse(json).RootElement;
    }
}

using Application.Models;
using Application.Services;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.IntegrationTests.Indexer;

[TestFixture]
public sealed class ElasticsearchIndexerTests : IntegrationFixture
{
    private IServiceScope _scope = null!;
    private IElasticsearchIndexer _indexer = null!;

    [SetUp]
    public void SetUp()
    {
        _scope   = CreateScope();
        _indexer = _scope.ServiceProvider.GetRequiredService<IElasticsearchIndexer>();
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    private static ProductSearchDocument BuildDocument(
        string? id = null,
        int stock  = 5,
        string status = "Draft") => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        Name = "Test Product",
        Description = "A test product",
        CategoryId = Guid.NewGuid().ToString(),
        Price = 99.99m,
        Currency = "USD",
        Stock = stock,
        Status = status,
        SellerId = Guid.NewGuid().ToString(),
        CreatedAt = DateTime.UtcNow,
        Attributes = new Dictionary<string, string> { ["color"] = "blue" },
        ImageUrls = ["https://example.com/img.jpg"],
    };

    #region UpsertAsync

    [Test]
    public async Task UpsertAsync_ShouldIndexDocument_AndMakeItRetrievable()
    {
        var document = BuildDocument();

        await _indexer.UpsertAsync(document);

        var response = await GetDocumentAsync(document.Id);

        response.Found.Should().BeTrue("UpsertAsync must create the document in Elasticsearch");
        response.Source!.Name.Should().Be("Test Product");
        response.Source.Price.Should().Be(99.99m);
        response.Source.Currency.Should().Be("USD");
        response.Source.SellerId.Should().Be(document.SellerId);
        response.Source.Status.Should().Be("Draft");
        response.Source.Stock.Should().Be(5);
    }

    [Test]
    public async Task UpsertAsync_CalledTwice_ShouldReplaceDocument()
    {
        var id = Guid.NewGuid().ToString();

        await _indexer.UpsertAsync(BuildDocument(id: id, status: "Draft",  stock: 10));
        await _indexer.UpsertAsync(BuildDocument(id: id, status: "Active", stock: 20));

        var response = await GetDocumentAsync(id);

        response.Source!.Status.Should().Be("Active", "second upsert must fully replace the document");
        response.Source.Stock.Should().Be(20);
    }

    [Test]
    public async Task UpsertAsync_ShouldStoreAttributes_AsSearchableMap()
    {
        var id = Guid.NewGuid().ToString();
        var doc = BuildDocument(id: id);
        doc.Attributes = new Dictionary<string, string> { ["layout"] = "tenkeyless", ["switch"] = "mx-red" };

        await _indexer.UpsertAsync(doc);

        var response = await GetDocumentAsync(id);

        response.Source!.Attributes.Should().ContainKey("layout");
        response.Source.Attributes["layout"].Should().Be("tenkeyless");
        response.Source.Attributes["switch"].Should().Be("mx-red");
    }

    #endregion

    #region UpdateFieldsAsync

    [Test]
    public async Task UpdateFieldsAsync_ShouldUpdateNamePriceAndCategory()
    {
        var id       = Guid.NewGuid().ToString();
        var newCatId = Guid.NewGuid().ToString();

        await _indexer.UpsertAsync(BuildDocument(id: id));

        await _indexer.UpdateFieldsAsync(id, new ProductFieldsUpdateDocument
        {
            Name = "Updated Name",
            Description = "Updated Desc",
            CategoryId  = newCatId,
            Price = 199.99m,
            Currency = "EUR",
            Attributes = new Dictionary<string, string>(),
            ImageUrls = [],
            UpdatedAt = DateTime.UtcNow,
        });

        var response = await GetDocumentAsync(id);
        var source   = response.Source!;

        source.Name.Should().Be("Updated Name");
        source.Description.Should().Be("Updated Desc");
        source.CategoryId.Should().Be(newCatId);
        source.Price.Should().Be(199.99m);
        source.Currency.Should().Be("EUR");
    }

    [Test]
    public async Task UpdateFieldsAsync_ShouldNotChangeStockOrStatus()
    {
        var id = Guid.NewGuid().ToString();
        await _indexer.UpsertAsync(BuildDocument(id: id, stock: 42, status: "Active"));

        await _indexer.UpdateFieldsAsync(id, new ProductFieldsUpdateDocument
        {
            Name = "New Name",
            Description = "New Desc",
            CategoryId = Guid.NewGuid().ToString(),
            Price = 49.99m,
            Currency = "USD",
            Attributes = new Dictionary<string, string>(),
            ImageUrls = [],
            UpdatedAt = DateTime.UtcNow,
        });

        var response = await GetDocumentAsync(id);
        var source   = response.Source!;

        source.Stock.Should().Be(42,       "UpdateFieldsAsync must not touch the stock field");
        source.Status.Should().Be("Active", "UpdateFieldsAsync must not touch the status field");
    }

    #endregion

    #region UpdateStockAsync

    [Test]
    public async Task UpdateStockAsync_ShouldChangStockValue()
    {
        var id = Guid.NewGuid().ToString();
        await _indexer.UpsertAsync(BuildDocument(id: id, stock: 10));

        await _indexer.UpdateStockAsync(id, new StockUpdateDocument { Stock = 99 });

        var response = await GetDocumentAsync(id);

        response.Source!.Stock.Should().Be(99);
    }

    [Test]
    public async Task UpdateStockAsync_ShouldNotChangeNameOrStatus()
    {
        var id = Guid.NewGuid().ToString();
        await _indexer.UpsertAsync(BuildDocument(id: id, stock: 10, status: "Active"));

        await _indexer.UpdateStockAsync(id, new StockUpdateDocument { Stock = 0 });

        var response = await GetDocumentAsync(id);
        var source   = response.Source!;

        source.Name.Should().Be("Test Product", "UpdateStockAsync must not touch other fields");
        source.Status.Should().Be("Active", "UpdateStockAsync must not touch the status field");
    }

    #endregion

    #region UpdateStatusAsync

    [Test]
    public async Task UpdateStatusAsync_ShouldChangeStatus()
    {
        var id = Guid.NewGuid().ToString();
        await _indexer.UpsertAsync(BuildDocument(id: id, status: "Draft"));

        await _indexer.UpdateStatusAsync(id, new StatusUpdateDocument { Status = "Active" });

        var response = await GetDocumentAsync(id);

        response.Source!.Status.Should().Be("Active");
    }

    [Test]
    public async Task UpdateStatusAsync_ShouldNotChangeStockOrName()
    {
        var id = Guid.NewGuid().ToString();
        await _indexer.UpsertAsync(BuildDocument(id: id, stock: 7, status: "Draft"));

        await _indexer.UpdateStatusAsync(id, new StatusUpdateDocument { Status = "Inactive" });

        var response = await GetDocumentAsync(id);
        var source   = response.Source!;

        source.Stock.Should().Be(7, "UpdateStatusAsync must not touch the stock field");
        source.Name.Should().Be("Test Product", "UpdateStatusAsync must not touch other fields");
    }

    #endregion

    #region DeleteAsync

    [Test]
    public async Task DeleteAsync_ShouldRemoveDocument_FromIndex()
    {
        var id = Guid.NewGuid().ToString();
        await _indexer.UpsertAsync(BuildDocument(id: id));

        await _indexer.DeleteAsync(id);

        var response = await GetDocumentAsync(id);

        response.Found.Should().BeFalse("DeleteAsync must remove the document from Elasticsearch");
    }

    [Test]
    public async Task DeleteAsync_WhenDocumentDoesNotExist_ShouldNotThrow()
    {
        var nonExistentId = Guid.NewGuid().ToString();

        // ElasticsearchIndexer treats NotFound as acceptable (idempotent delete)
        Assert.DoesNotThrowAsync(() => _indexer.DeleteAsync(nonExistentId));
    }

    #endregion
}

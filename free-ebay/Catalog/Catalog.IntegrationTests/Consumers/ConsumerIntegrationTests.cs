using System.Text.Json;
using Application.Consumers;
using Application.Models;
using Application.Services;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Catalog.IntegrationTests.Consumers;

[TestFixture]
public sealed class ConsumerIntegrationTests : IntegrationFixture
{
    private IServiceScope _scope = null!;
    private IElasticsearchIndexer _indexer = null!;

    [SetUp]
    public void SetUp()
    {
        _scope = CreateScope();
        _indexer = _scope.ServiceProvider.GetRequiredService<IElasticsearchIndexer>();
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    #region ProductCreatedConsumer

    [Test]
    public async Task ProductCreatedConsumer_ShouldIndexDocument_WithAllCorrectFields()
    {
        var productId  = Guid.NewGuid();
        var sellerId   = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var createdAt  = new DateTime(2025, 3, 1, 10, 0, 0, DateTimeKind.Utc);

        var payload  = CreatedPayload(productId, sellerId, categoryId,
            "Mechanical Keyboard", "Tenkeyless layout", 149.99m, "USD", 3,
            attributes: [new { Key = "switch", Value   = "mx-red" }],
            imageUrls: ["https://img.example.com/kb.jpg"], createdAt);

        var consumer = new ProductCreatedConsumer(_indexer, NullLogger<ProductCreatedConsumer>.Instance);
        await consumer.ConsumeAsync(payload, CancellationToken.None);

        var response = await GetDocumentAsync(productId.ToString());

        response.Found.Should().BeTrue("ProductCreatedConsumer must create an Elasticsearch document");
        var source = response.Source!;
        source.Id.Should().Be(productId.ToString());
        source.Name.Should().Be("Mechanical Keyboard");
        source.Description.Should().Be("Tenkeyless layout");
        source.Price.Should().Be(149.99m);
        source.Currency.Should().Be("USD");
        source.Stock.Should().Be(3);
        source.SellerId.Should().Be(sellerId.ToString());
        source.CategoryId.Should().Be(categoryId.ToString());
        source.Status.Should().Be("Draft", "new products are always indexed with status Draft");
        source.CreatedAt.Should().BeCloseTo(createdAt, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task ProductCreatedConsumer_ShouldMapAttributes_ToDictionary()
    {
        var productId = Guid.NewGuid();
        var payload   = CreatedPayload(productId, Guid.NewGuid(), Guid.NewGuid(),
            "Gaming Mouse", "RGB", 59.99m, "USD", 10,
            attributes: [new { Key = "dpi", Value = "16000" }, new { Key = "buttons", Value = "6" }],
            imageUrls: [], DateTime.UtcNow);

        var consumer = new ProductCreatedConsumer(_indexer, NullLogger<ProductCreatedConsumer>.Instance);
        await consumer.ConsumeAsync(payload, CancellationToken.None);

        var response = await GetDocumentAsync(productId.ToString());

        response.Source!.Attributes["dpi"].Should().Be("16000");
        response.Source.Attributes["buttons"].Should().Be("6");
    }

    #endregion

    #region ProductUpdatedConsumer

    [Test]
    public async Task ProductUpdatedConsumer_ShouldUpdateNamePriceAndCurrency()
    {
        var productId = Guid.NewGuid().ToString();
        await _indexer.UpsertAsync(SeedDocument(productId, stock: 42, status: "Active"));

        var payload = UpdatedPayload(
            Guid.Parse(productId), Guid.NewGuid(),
            "Updated Name", "Updated Desc", 199.99m, "EUR",
            attributes: [], DateTime.UtcNow);

        var consumer = new ProductUpdatedConsumer(_indexer, NullLogger<ProductUpdatedConsumer>.Instance);
        await consumer.ConsumeAsync(payload, CancellationToken.None);

        var response = await GetDocumentAsync(productId);
        var source   = response.Source!;

        source.Name.Should().Be("Updated Name");
        source.Description.Should().Be("Updated Desc");
        source.Price.Should().Be(199.99m);
        source.Currency.Should().Be("EUR");
    }

    [Test]
    public async Task ProductUpdatedConsumer_ShouldPreserveStockAndStatus()
    {
        var productId = Guid.NewGuid().ToString();
        await _indexer.UpsertAsync(SeedDocument(productId, stock: 42, status: "Active"));

        var payload = UpdatedPayload(
            Guid.Parse(productId), Guid.NewGuid(),
            "Name", "Desc", 10m, "USD", attributes: [], DateTime.UtcNow);

        var consumer = new ProductUpdatedConsumer(_indexer, NullLogger<ProductUpdatedConsumer>.Instance);
        await consumer.ConsumeAsync(payload, CancellationToken.None);

        var response = await GetDocumentAsync(productId);
        var source   = response.Source!;

        source.Stock.Should().Be(42,       "ProductUpdatedConsumer must not modify stock");
        source.Status.Should().Be("Active", "ProductUpdatedConsumer must not modify status");
    }

    #endregion

    #region ProductStockUpdatedConsumer

    [Test]
    public async Task ProductStockUpdatedConsumer_ShouldUpdateStockToNewQuantity()
    {
        var productId = Guid.NewGuid().ToString();
        await _indexer.UpsertAsync(SeedDocument(productId, stock: 20, status: "Active"));

        var payload = StockPayload(Guid.Parse(productId), previousQuantity: 20, newQuantity: 0);

        var consumer = new ProductStockUpdatedConsumer(_indexer, NullLogger<ProductStockUpdatedConsumer>.Instance);
        await consumer.ConsumeAsync(payload, CancellationToken.None);

        var response = await GetDocumentAsync(productId);

        response.Source!.Stock.Should()
            .Be(0, "consumer must use NewQuantity, not PreviousQuantity");
    }

    [Test]
    public async Task ProductStockUpdatedConsumer_ShouldNotChangeStatus()
    {
        var productId = Guid.NewGuid().ToString();
        await _indexer.UpsertAsync(SeedDocument(productId, stock: 5, status: "Active"));

        var payload = StockPayload(Guid.Parse(productId), previousQuantity: 5, newQuantity: 99);

        var consumer = new ProductStockUpdatedConsumer(_indexer, NullLogger<ProductStockUpdatedConsumer>.Instance);
        await consumer.ConsumeAsync(payload, CancellationToken.None);

        var response = await GetDocumentAsync(productId);

        response.Source!.Status.Should().Be("Active",
            "ProductStockUpdatedConsumer must not modify status");
    }

    #endregion

    #region ProductStatusChangedConsumer

    [Test]
    public async Task ProductStatusChangedConsumer_ShouldUpdateStatusToNewStatus()
    {
        var productId = Guid.NewGuid().ToString();
        await _indexer.UpsertAsync(SeedDocument(productId, stock: 5, status: "Draft"));

        var payload = StatusPayload(Guid.Parse(productId), "Draft", "Active");

        var consumer = new ProductStatusChangedConsumer(_indexer, NullLogger<ProductStatusChangedConsumer>.Instance);
        await consumer.ConsumeAsync(payload, CancellationToken.None);

        var response = await GetDocumentAsync(productId);

        response.Source!.Status.Should().Be("Active",
            "consumer must use NewStatus, not PreviousStatus");
    }

    [Test]
    public async Task ProductStatusChangedConsumer_ShouldNotChangeStock()
    {
        var productId = Guid.NewGuid().ToString();
        await _indexer.UpsertAsync(SeedDocument(productId, stock: 7, status: "Active"));

        var payload = StatusPayload(Guid.Parse(productId), "Active", "Inactive");

        var consumer = new ProductStatusChangedConsumer(_indexer, NullLogger<ProductStatusChangedConsumer>.Instance);
        await consumer.ConsumeAsync(payload, CancellationToken.None);

        var response = await GetDocumentAsync(productId);

        response.Source!.Stock.Should().Be(7,
            "ProductStatusChangedConsumer must not modify stock");
    }

    #endregion

    #region ProductDeletedConsumer

    [Test]
    public async Task ProductDeletedConsumer_ShouldRemoveDocument_FromElasticsearch()
    {
        var productId = Guid.NewGuid().ToString();
        await _indexer.UpsertAsync(SeedDocument(productId));

        var payload = DeletedPayload(Guid.Parse(productId));

        var consumer = new ProductDeletedConsumer(_indexer, NullLogger<ProductDeletedConsumer>.Instance);
        await consumer.ConsumeAsync(payload, CancellationToken.None);

        var response = await GetDocumentAsync(productId);

        response.Found.Should().BeFalse(
            "ProductDeletedConsumer must remove the document from Elasticsearch");
    }

    [Test]
    public async Task ProductCreatedThenDeleted_ShouldLeaveNoDocument()
    {
        var productId  = Guid.NewGuid();
        var sellerId   = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var createConsumer = new ProductCreatedConsumer(_indexer, NullLogger<ProductCreatedConsumer>.Instance);
        await createConsumer.ConsumeAsync(
            CreatedPayload(productId, sellerId, categoryId, "Temp", "Temp", 10m, "USD", 1, [], [], DateTime.UtcNow),
            CancellationToken.None);

        var deleteConsumer = new ProductDeletedConsumer(_indexer, NullLogger<ProductDeletedConsumer>.Instance);
        await deleteConsumer.ConsumeAsync(DeletedPayload(productId), CancellationToken.None);

        var response = await GetDocumentAsync(productId.ToString());

        response.Found.Should().BeFalse("document must not exist after create → delete sequence");
    }

    #endregion

    // ----------helpers----------

    private static ProductSearchDocument SeedDocument(string id, int stock = 5, string status = "Draft") => new()
    {
        Id = id,
        Name = "Seeded Product",
        Description = "Seeded for integration test",
        CategoryId = Guid.NewGuid().ToString(),
        Price = 99.99m,
        Currency = "USD",
        Stock = stock,
        Status = status,
        SellerId = Guid.NewGuid().ToString(),
        CreatedAt = DateTime.UtcNow,
        Attributes = new Dictionary<string, string>(),
        ImageUrls = [],
    };

    private static JsonElement CreatedPayload(
        Guid productId, Guid sellerId, Guid categoryId,
        string name, string description, decimal price, string currency, int stock,
        object[] attributes, string[] imageUrls, DateTime createdAt) =>
        Parse(new
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

    private static JsonElement UpdatedPayload(
        Guid productId, Guid categoryId,
        string name, string description, decimal price, string currency,
        object[] attributes, DateTime updatedAt) =>
        Parse(new
        {
            ProductId = new { Value = productId },
            Name = name,
            Description = description,
            CategoryId = new { Value = categoryId },
            Price = new { Amount = price, Currency = currency },
            Attributes = attributes,
            ImageUrls = Array.Empty<string>(),
            UpdatedAt = updatedAt,
        });

    private static JsonElement StockPayload(Guid productId, int previousQuantity, int newQuantity) =>
        Parse(new
        {
            ProductId = new { Value = productId },
            PreviousQuantity = previousQuantity,
            NewQuantity = newQuantity,
        });

    private static JsonElement StatusPayload(Guid productId, string previousStatus, string newStatus) =>
        Parse(new
        {
            ProductId = new { Value = productId },
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
        });

    private static JsonElement DeletedPayload(Guid productId) =>
        Parse(new { ProductId = new { Value = productId } });

    private static JsonElement Parse(object obj) =>
        JsonDocument.Parse(JsonSerializer.Serialize(obj)).RootElement;
}

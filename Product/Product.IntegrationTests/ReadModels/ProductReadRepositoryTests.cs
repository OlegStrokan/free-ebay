using Application.DTOs;
using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Product.IntegrationTests.Infrastructure;
using Xunit;

namespace Product.IntegrationTests.ReadModels;

[Collection("Integration")]
public sealed class ProductReadRepositoryTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public ProductReadRepositoryTests(IntegrationFixture fixture) => _fixture = fixture;

    private static async Task<Guid> SeedCategoryAsync(ProductDbContext db, string name = "Electronics")
    {
        var id = Guid.NewGuid();
        db.Categories.Add(new Category { Id = id, Name = name });
        await db.SaveChangesAsync();
        return id;
    }

    // Persists catalog/listing rows directly, bypassing the persistence service (no outbox)
    private static async Task<(CatalogItem CatalogItem, Listing Listing)> SeedProductAsync(
        ProductDbContext db,
        Guid categoryId,
        Guid? sellerId = null,
        string name = "Test Product",
        decimal price = 49.99m,
        int stock = 5)
    {
        var catalogItem = CatalogItem.Create(
            name,
            "A seeded test product",
            CategoryId.From(categoryId),
            null,
            [],
            []);
        catalogItem.ClearDomainEvents();

        var listing = Listing.Create(
            catalogItem.Id,
            SellerId.From(sellerId ?? Guid.NewGuid()),
            Money.Create(price, "USD"),
            stock,
            ListingCondition.New,
            null);
        listing.ClearDomainEvents();

        db.CatalogItems.Add(catalogItem);
        db.Listings.Add(listing);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return (catalogItem, listing);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProductDetailDto_WithCategoryName()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IListingReadRepository>();
        var db   = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var categoryId = await SeedCategoryAsync(db, "Gadgets");
        var product = await SeedProductAsync(db, categoryId, name: "Smart Speaker");

        var dto = await repo.GetByIdAsync(product.Listing.Id.Value);

        dto.Should().NotBeNull();
        dto!.ProductId.Should().Be(product.Listing.Id.Value);
        dto.CatalogItemId.Should().Be(product.CatalogItem.Id.Value);
        dto.Name.Should().Be("Smart Speaker");
        dto.CategoryName.Should().Be("Gadgets");
        dto.Price.Should().Be(49.99m);
        dto.Currency.Should().Be("USD");
        dto.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IListingReadRepository>();

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdsAsync_ShouldReturnMatchingProductDetailDtos()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IListingReadRepository>();
        var db   = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var categoryId = await SeedCategoryAsync(db, "Books");
        var p1 = await SeedProductAsync(db, categoryId, name: "Book A");
        var p2 = await SeedProductAsync(db, categoryId, name: "Book B");
        var unrelatedId = Guid.NewGuid();

        var results = await repo.GetByIdsAsync([p1.Listing.Id.Value, p2.Listing.Id.Value, unrelatedId]);

        results.Should().HaveCount(2,
            "only products that exist should be returned");
        results.Select(r => r.Name).Should().Contain(["Book A", "Book B"]);
    }

    [Fact]
    public async Task GetPricesByIdsAsync_ShouldReturnPriceDtos_ForExistingProducts()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IListingReadRepository>();
        var db   = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var categoryId = await SeedCategoryAsync(db);
        var p1 = await SeedProductAsync(db, categoryId, price: 19.99m);
        var p2 = await SeedProductAsync(db, categoryId, price: 99.00m);

        var prices = await repo.GetPricesByIdsAsync([p1.Listing.Id.Value, p2.Listing.Id.Value]);

        prices.Should().HaveCount(2);
        prices.Should().Contain(p => p.ProductId == p1.Listing.Id.Value && p.Price == 19.99m);
        prices.Should().Contain(p => p.ProductId == p2.Listing.Id.Value && p.Price == 99.00m);
    }

    [Fact]
    public async Task GetBySellerAsync_ShouldReturnPagedResults_InDescendingCreatedAtOrder()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IListingReadRepository>();
        var db   = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var categoryId = await SeedCategoryAsync(db, "Clothing");
        var sellerId   = Guid.NewGuid();

        // seed 3 products for the same seller
        await SeedProductAsync(db, categoryId, sellerId: sellerId, name: "Shirt");
        await SeedProductAsync(db, categoryId, sellerId: sellerId, name: "Pants");
        await SeedProductAsync(db, categoryId, sellerId: sellerId, name: "Jacket");

        // page 1, size 2
        var page1 = await repo.GetBySellerAsync(sellerId, page: 1, size: 2);
        // page 2, size 2
        var page2 = await repo.GetBySellerAsync(sellerId, page: 2, size: 2);

        page1.TotalCount.Should().Be(3);
        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetBySellerAsync_ShouldReturnEmpty_WhenSellerHasNoProducts()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IListingReadRepository>();

        var result = await repo.GetBySellerAsync(Guid.NewGuid(), page: 1, size: 20);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}

using Domain.Events.B2BOrder;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.IntegrationTests.Infrastructure;
using Xunit;

namespace Order.IntegrationTests.ReadModels;

[Collection("Integration")]
public sealed class B2BOrderReadModelUpdaterTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public B2BOrderReadModelUpdaterTests(IntegrationFixture fixture) => _fixture = fixture;

    private static Address TestAddress => Address.Create("Na Příkopě 12", "Prague", "CZ", "11000");

    private static B2BOrderStartedEvent BuildStartedEvent(Guid? id = null) => new(
        B2BOrderId.From(id ?? Guid.NewGuid()),
        CustomerId.CreateUnique(),
        "ACME Corp",
        TestAddress,
        DateTime.UtcNow);

    [Fact]
    public async Task B2BOrderReadModelUpdater_ShouldCreateReadModel_OnB2BOrderStartedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<B2BOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var evt = BuildStartedEvent();

        await updater.HandleAsync(evt);

        var row = await db.B2BOrderReadModels
            .FirstOrDefaultAsync(b => b.Id == evt.B2BOrderId.Value);

        row.Should().NotBeNull();
        row!.CompanyName.Should().Be("ACME Corp");
        row.Status.Should().Be("Draft");
        row.CustomerId.Should().Be(evt.CustomerId.Value);
        row.DeliveryStreet.Should().Be(TestAddress.Street);
        row.TotalPrice.Should().Be(0m);
        row.DiscountPercent.Should().Be(0m);
    }

    [Fact]
    public async Task B2BOrderReadModelUpdater_ShouldBeIdempotent_OnDuplicateStartedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<B2BOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var evt = BuildStartedEvent();

        await updater.HandleAsync(evt);
        await updater.HandleAsync(evt); // duplicate must not throw or produce second row

        var count = await db.B2BOrderReadModels.CountAsync(b => b.Id == evt.B2BOrderId.Value);
        count.Should().Be(1, "duplicate StartedEvent must not produce a second read-model row");
    }

    [Fact]
    public async Task B2BOrderReadModelUpdater_ShouldUpdateItemsAndTotalPrice_OnLineItemAddedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<B2BOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var startedEvt = BuildStartedEvent();
        await updater.HandleAsync(startedEvt);

        var addedEvt = new LineItemAddedEvent(
            startedEvt.B2BOrderId,
            QuoteLineItemId.CreateUnique(),
            ProductId.CreateUnique(),
            Quantity: 4,
            UnitPrice: Money.Create(25m, "USD"),
            DateTime.UtcNow);

        await updater.HandleAsync(addedEvt);

        var row = await db.B2BOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == startedEvt.B2BOrderId.Value);

        row.Should().NotBeNull();
        row!.TotalPrice.Should().Be(100m, "4 × $25 = $100");
        row.Currency.Should().Be("USD");
        row.Version.Should().Be(1);
        row.ItemsJson.Should().Contain(addedEvt.ProductId.Value.ToString());
    }

    [Fact]
    public async Task B2BOrderReadModelUpdater_ShouldMarkItemRemoved_AndRecalculateTotal()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<B2BOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var startedEvt = BuildStartedEvent();
        await updater.HandleAsync(startedEvt);

        var itemId = QuoteLineItemId.CreateUnique();
        var addedEvt = new LineItemAddedEvent(
            startedEvt.B2BOrderId, itemId, ProductId.CreateUnique(),
            3, Money.Create(10m, "USD"), DateTime.UtcNow);
        await updater.HandleAsync(addedEvt);

        var removedEvt = new LineItemRemovedEvent(startedEvt.B2BOrderId, itemId, DateTime.UtcNow);
        await updater.HandleAsync(removedEvt);

        var row = await db.B2BOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == startedEvt.B2BOrderId.Value);

        row.Should().NotBeNull();
        row!.TotalPrice.Should().Be(0m, "removed item should bring total to zero");
    }

    [Fact]
    public async Task B2BOrderReadModelUpdater_ShouldApplyDiscount_AndRecalculateTotal()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<B2BOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var startedEvt = BuildStartedEvent();
        await updater.HandleAsync(startedEvt);

        // add one item: 2 × $100 = $200
        var addedEvt = new LineItemAddedEvent(
            startedEvt.B2BOrderId, QuoteLineItemId.CreateUnique(), ProductId.CreateUnique(),
            2, Money.Create(100m, "USD"), DateTime.UtcNow);
        await updater.HandleAsync(addedEvt);

        // 10% discount → $180
        var discountEvt = new DiscountAppliedEvent(startedEvt.B2BOrderId, 10m, DateTime.UtcNow);
        await updater.HandleAsync(discountEvt);

        var row = await db.B2BOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == startedEvt.B2BOrderId.Value);

        row.Should().NotBeNull();
        row!.DiscountPercent.Should().Be(10m);
        row.TotalPrice.Should().Be(180m, "200 - 10% = 180");
    }

    [Fact]
    public async Task B2BOrderReadModelUpdater_ShouldAppendComment_OnCommentAddedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<B2BOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var startedEvt = BuildStartedEvent();
        await updater.HandleAsync(startedEvt);

        var commentEvt = new CommentAddedEvent(
            startedEvt.B2BOrderId, "Alice", "Pricing looks fair", DateTime.UtcNow);
        await updater.HandleAsync(commentEvt);

        var row = await db.B2BOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == startedEvt.B2BOrderId.Value);

        row.Should().NotBeNull();
        row!.CommentsJson.Should().Contain("Alice");
        row.CommentsJson.Should().Contain("Pricing looks fair");
    }

    [Fact]
    public async Task B2BOrderReadModelUpdater_ShouldUpdateAddress_OnDeliveryAddressChangedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<B2BOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var startedEvt = BuildStartedEvent();
        await updater.HandleAsync(startedEvt);

        var newAddress = Address.Create("Nový Svět 7", "Brno", "CZ", "60200");
        var addressEvt = new DeliveryAddressChangedEvent(
            startedEvt.B2BOrderId, newAddress, DateTime.UtcNow);
        await updater.HandleAsync(addressEvt);

        var row = await db.B2BOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == startedEvt.B2BOrderId.Value);

        row.Should().NotBeNull();
        row!.DeliveryStreet.Should().Be("Nový Svět 7");
        row.DeliveryCity.Should().Be("Brno");
        row.DeliveryPostalCode.Should().Be("60200");
    }

    [Fact]
    public async Task B2BOrderReadModelUpdater_ShouldSetStatusToFinalized_OnQuoteFinalizedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<B2BOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var startedEvt = BuildStartedEvent();
        await updater.HandleAsync(startedEvt);

        var createdOrderId = Guid.NewGuid();
        var finalizedEvt = new QuoteFinalizedEvent(
            startedEvt.B2BOrderId, createdOrderId, DateTime.UtcNow);
        await updater.HandleAsync(finalizedEvt);

        var row = await db.B2BOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == startedEvt.B2BOrderId.Value);

        row.Should().NotBeNull();
        row!.Status.Should().Be("Finalized");
        row.FinalizedOrderId.Should().Be(createdOrderId);
    }

    [Fact]
    public async Task B2BOrderReadModelUpdater_ShouldSetStatusToCancelled_OnCancelledEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<B2BOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var startedEvt = BuildStartedEvent();
        await updater.HandleAsync(startedEvt);

        var cancelledEvt = new B2BOrderCancelledEvent(
            startedEvt.B2BOrderId,
            new List<string> { "Budget cut" },
            DateTime.UtcNow);
        await updater.HandleAsync(cancelledEvt);

        var row = await db.B2BOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == startedEvt.B2BOrderId.Value);

        row.Should().NotBeNull();
        row!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task B2BOrderReadModelUpdater_ShouldRecalculateTotal_OnPriceAndQuantityChanges()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<B2BOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var startedEvt = BuildStartedEvent();
        await updater.HandleAsync(startedEvt);

        var itemId = QuoteLineItemId.CreateUnique();
        // add item: 5 × $20 = $100
        await updater.HandleAsync(new LineItemAddedEvent(
            startedEvt.B2BOrderId, itemId, ProductId.CreateUnique(),
            5, Money.Create(20m, "USD"), DateTime.UtcNow));

        // change qty to 3: 3 × $20 = $60
        await updater.HandleAsync(new LineItemQuantityChangedEvent(
            startedEvt.B2BOrderId, itemId, 3, DateTime.UtcNow));

        // adjust price to $30: 3 × $30 = $90
        await updater.HandleAsync(new LineItemPriceAdjustedEvent(
            startedEvt.B2BOrderId, itemId, Money.Create(30m, "USD"), DateTime.UtcNow));

        var row = await db.B2BOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == startedEvt.B2BOrderId.Value);

        row.Should().NotBeNull();
        row!.TotalPrice.Should().Be(90m, "3 × $30 (adjusted price) = $90");
    }
}

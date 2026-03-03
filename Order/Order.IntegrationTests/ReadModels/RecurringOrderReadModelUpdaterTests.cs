using Domain.Entities.Subscription;
using Domain.Events.Subscription;
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
public sealed class RecurringOrderReadModelUpdaterTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public RecurringOrderReadModelUpdaterTests(IntegrationFixture fixture) => _fixture = fixture;

    private static Address TestAddress => Address.Create("Wenceslas Sq 1", "Prague", "CZ", "11000");

    private static List<RecurringOrderItem> DefaultItems() =>
        new() { RecurringOrderItem.Create(ProductId.CreateUnique(), 2, Money.Create(50m, "USD")) };

    private static RecurringOrderCreatedEvent BuildCreatedEvent(Guid? id = null, int? maxExecutions = null) => new(
        RecurringOrderId.From(id ?? Guid.NewGuid()),
        CustomerId.CreateUnique(),
        Frequency: "Weekly",
        Items: DefaultItems(),
        DeliveryAddress: TestAddress,
        PaymentMethod: "Card-99",
        NextRunAt: DateTime.UtcNow.AddDays(7),
        MaxExecutions: maxExecutions,
        CreatedAt: DateTime.UtcNow);
    
    [Fact]
    public async Task RecurringOrderReadModelUpdater_ShouldCreateReadModel_OnCreatedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<RecurringOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var evt = BuildCreatedEvent();

        await updater.HandleAsync(evt);

        var row = await db.RecurringOrderReadModels
            .FirstOrDefaultAsync(r => r.Id == evt.RecurringOrderId.Value);

        row.Should().NotBeNull();
        row!.Status.Should().Be("Active");
        row.CustomerId.Should().Be(evt.CustomerId.Value);
        row.Frequency.Should().Be("Weekly");
        row.PaymentMethod.Should().Be("Card-99");
        row.TotalExecutions.Should().Be(0);
        row.LastRunAt.Should().BeNull();
        row.DeliveryStreet.Should().Be(TestAddress.Street);
        row.DeliveryCity.Should().Be(TestAddress.City);
        row.DeliveryCountry.Should().Be(TestAddress.Country);
        row.DeliveryPostalCode.Should().Be(TestAddress.PostalCode);
        row.ItemsJson.Should().NotBeNullOrEmpty();
        row.Version.Should().Be(0);
    }

    [Fact]
    public async Task RecurringOrderReadModelUpdater_ShouldBeIdempotent_OnDuplicateCreatedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<RecurringOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var evt = BuildCreatedEvent();

        await updater.HandleAsync(evt);
        await updater.HandleAsync(evt); // duplicate must not throw or insert a second row

        var count = await db.RecurringOrderReadModels
            .CountAsync(r => r.Id == evt.RecurringOrderId.Value);

        count.Should().Be(1, "duplicate CreatedEvent must not produce a second read-model row");
    }
    
    [Fact]
    public async Task RecurringOrderReadModelUpdater_ShouldSetPaused_OnPausedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<RecurringOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var createdEvt = BuildCreatedEvent();
        await updater.HandleAsync(createdEvt);

        var pausedEvt = new RecurringOrderPausedEvent(
            createdEvt.RecurringOrderId,
            PausedAt: DateTime.UtcNow);

        await updater.HandleAsync(pausedEvt);

        var row = await db.RecurringOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == createdEvt.RecurringOrderId.Value);

        row.Should().NotBeNull();
        row!.Status.Should().Be("Paused");
        row.Version.Should().Be(1);
    }
    
    [Fact]
    public async Task RecurringOrderReadModelUpdater_ShouldSetActive_OnResumedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<RecurringOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var createdEvt = BuildCreatedEvent();
        await updater.HandleAsync(createdEvt);
        await updater.HandleAsync(new RecurringOrderPausedEvent(createdEvt.RecurringOrderId, DateTime.UtcNow));

        var nextRunAt = DateTime.UtcNow.AddDays(7);
        var resumedEvt = new RecurringOrderResumedEvent(
            createdEvt.RecurringOrderId,
            NextRunAt: nextRunAt,
            ResumedAt: DateTime.UtcNow);

        await updater.HandleAsync(resumedEvt);

        var row = await db.RecurringOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == createdEvt.RecurringOrderId.Value);

        row.Should().NotBeNull();
        row!.Status.Should().Be("Active");
        row.NextRunAt.Should().BeCloseTo(nextRunAt, TimeSpan.FromSeconds(1));
        row.Version.Should().Be(2);
    }
    
    [Fact]
    public async Task RecurringOrderReadModelUpdater_ShouldSetCancelled_OnCancelledEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<RecurringOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var createdEvt = BuildCreatedEvent();
        await updater.HandleAsync(createdEvt);

        var cancelledEvt = new RecurringOrderCancelledEvent(
            createdEvt.RecurringOrderId,
            Reason: "Customer cancelled",
            CancelledAt: DateTime.UtcNow);

        await updater.HandleAsync(cancelledEvt);

        var row = await db.RecurringOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == createdEvt.RecurringOrderId.Value);

        row.Should().NotBeNull();
        row!.Status.Should().Be("Cancelled");
        row.Version.Should().Be(1);
    }
    
    [Fact]
    public async Task RecurringOrderReadModelUpdater_ShouldIncrementExecutions_OnExecutedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<RecurringOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var createdEvt = BuildCreatedEvent();
        await updater.HandleAsync(createdEvt);

        var executedAt = DateTime.UtcNow;
        var nextRunAt  = executedAt.AddDays(7);

        var executedEvt = new RecurringOrderExecutedEvent(
            createdEvt.RecurringOrderId,
            CreatedOrderId: Guid.NewGuid(),
            ExecutionNumber: 1,
            NextRunAt: nextRunAt,
            ExecutedAt: executedAt);

        await updater.HandleAsync(executedEvt);

        var row = await db.RecurringOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == createdEvt.RecurringOrderId.Value);

        row.Should().NotBeNull();
        row!.TotalExecutions.Should().Be(1);
        row.LastRunAt.Should().BeCloseTo(executedAt, TimeSpan.FromSeconds(1));
        row.NextRunAt.Should().BeCloseTo(nextRunAt, TimeSpan.FromSeconds(1));
        row.Status.Should().Be("Active");
        row.Version.Should().Be(1);
    }

    [Fact]
    public async Task RecurringOrderReadModelUpdater_ShouldAutoCancelOnMaxExecutionsReached()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<RecurringOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var createdEvt = BuildCreatedEvent(maxExecutions: 1);
        await updater.HandleAsync(createdEvt);

        var executedEvt = new RecurringOrderExecutedEvent(
            createdEvt.RecurringOrderId,
            CreatedOrderId: Guid.NewGuid(),
            ExecutionNumber: 1,
            NextRunAt: DateTime.UtcNow.AddDays(7),
            ExecutedAt: DateTime.UtcNow);

        await updater.HandleAsync(executedEvt);

        var row = await db.RecurringOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == createdEvt.RecurringOrderId.Value);

        row.Should().NotBeNull();
        row!.Status.Should().Be("Cancelled",
            "read model must auto-cancel when MaxExecutions is reached");
        row.TotalExecutions.Should().Be(1);
    }
    
    [Fact]
    public async Task RecurringOrderReadModelUpdater_ShouldRescheduleNextRunAt_OnExecutionFailedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<RecurringOrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var createdEvt = BuildCreatedEvent();
        await updater.HandleAsync(createdEvt);

        var nextRetryAt = DateTime.UtcNow.AddHours(1);

        var failedEvt = new RecurringOrderExecutionFailedEvent(
            createdEvt.RecurringOrderId,
            Reason: "Payment declined",
            NextRetryAt: nextRetryAt,
            FailedAt: DateTime.UtcNow);

        await updater.HandleAsync(failedEvt);

        var row = await db.RecurringOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == createdEvt.RecurringOrderId.Value);

        row.Should().NotBeNull();
        row!.NextRunAt.Should().BeCloseTo(nextRetryAt, TimeSpan.FromSeconds(1));
        row.Status.Should().Be("Active", "status must not change on a failed execution");
        row.Version.Should().Be(1);
    }
}

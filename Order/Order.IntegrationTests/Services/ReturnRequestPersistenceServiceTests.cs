using Application.Interfaces;
using Domain.Entities;
using Domain.Entities.Order;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.DbContext;
using Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.IntegrationTests.Infrastructure;
using Xunit;

namespace Order.IntegrationTests.Services;

[Collection("Integration")]
public sealed class ReturnRequestPersistenceServiceTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public ReturnRequestPersistenceServiceTests(IntegrationFixture fixture) => _fixture = fixture;
    
    private static (ReturnRequest Request, OrderId OrderId, CustomerId CustomerId) BuildReturnRequest()
    {
        var productId   = ProductId.CreateUnique();
        var orderItem   = OrderItem.Create(productId, 1, Money.Create(80, "USD"));
        var orderItems  = new List<OrderItem> { orderItem };
        var orderId     = OrderId.CreateUnique();
        var customerId  = CustomerId.CreateUnique();

        var request = ReturnRequest.Create(
            orderId,
            customerId,
            reason: "Item arrived damaged",
            itemsToReturn: orderItems,
            refundAmount: Money.Create(80, "USD"),
            orderCompletedAt: DateTime.UtcNow.AddDays(-1),   // completed yesterday
            orderItems: orderItems,
            returnWindow: TimeSpan.FromDays(14));             // 14-day window

        return (request, orderId, customerId);
    }
    
    private static async Task SeedReadModelAsync(AppDbContext db, ReturnRequest request)
    {
        db.ReturnRequestReadModels.Add(new ReturnRequestReadModel
        {
            Id           = request.Id.Value,
            OrderId      = request.OrderId.Value,
            CustomerId   = request.CustomerId.Value,
            Status       = "Pending",
            Reason       = request.Reason,
            RefundAmount = request.RefundAmount.Amount,
            Currency     = request.RefundAmount.Currency,
            ItemsToReturnJson = "[]",
            RequestedAt  = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
    
    [Fact]
    public async Task CreateReturnRequestAsync_ShouldSaveEventsAndOutboxMessage()
    {
        await using var scope = _fixture.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IReturnRequestPersistenceService>();
        var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (request, _, _) = BuildReturnRequest();
        
        await svc.CreateReturnRequestAsync(request, cancellationToken: CancellationToken.None);

        // Events are saved under the ReturnRequest's own aggregate ID (not the OrderId)
        var events = await db.DomainEvents
            .Where(e => e.AggregateId == request.Id.Value.ToString())
            .ToListAsync();

        var outbox = await db.OutboxMessages
            .Where(m => m.Id == events[0].EventId)
            .ToListAsync();

        events.Should().HaveCount(1, "only ReturnRequestCreatedEvent is raised on Create");
        outbox.Should().HaveCount(1,
            "outbox row must mirror every domain event");
    }
    
    [Fact]
    public async Task LoadByOrderIdAsync_ShouldFindReturnRequest_ViaReadModel()
    {
        await using var scope = _fixture.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IReturnRequestPersistenceService>();
        var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (request, orderId, _) = BuildReturnRequest();

        await svc.CreateReturnRequestAsync(request, cancellationToken: CancellationToken.None);

        // The read-model normally arrives via KafkaReadModelSynchronizer; seed it manually
        await SeedReadModelAsync(db, request);

        // lookup by the business orderId (not by the return-request id)
        var loaded = await svc.LoadByOrderIdAsync(orderId.Value, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Id.Value.Should().Be(request.Id.Value,
            "LoadByOrderIdAsync must reconstruct the same aggregate");
        loaded.OrderId.Value.Should().Be(orderId.Value);
        loaded.Reason.Should().Be(request.Reason);
    }


    /*
     * Task A loads v=0, mutates (MarkAsReceived), then holds.
     * Task B loads same v=0, commits MarkAsReceived at v=1.
     * Task A resumes → conflict → retry → reloads v=1 (already Received) → action is no-op
     * → 0 events → SaveEventsAsync bails early → commits cleanly.
     */
    [Fact]
    public async Task UpdateReturnRequestAsync_ShouldRetryAndSucceed_OnSingleConcurrencyConflict()
    {
        await using var setupScope = _fixture.CreateScope();
        var setupSvc = setupScope.ServiceProvider.GetRequiredService<IReturnRequestPersistenceService>();
        var setupDb  = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (request, orderId, _) = BuildReturnRequest();
        await setupSvc.CreateReturnRequestAsync(request, cancellationToken: CancellationToken.None);
        await SeedReadModelAsync(setupDb, request);

        await using var scopeA = _fixture.CreateScope();
        await using var scopeB = _fixture.CreateScope();
        var svcA = scopeA.ServiceProvider.GetRequiredService<IReturnRequestPersistenceService>();
        var svcB = scopeB.ServiceProvider.GetRequiredService<IReturnRequestPersistenceService>();

        var barrier     = new SemaphoreSlim(0, 1);
        var release     = new SemaphoreSlim(0, 1);
        var attemptCount = 0;

        var taskA = svcA.UpdateReturnRequestAsync(orderId.Value, async rr =>
        {
            if (Interlocked.Increment(ref attemptCount) == 1)
            {
                rr.MarkAsReceived();
                barrier.Release();         // signal: loaded + mutated, now holding
                await release.WaitAsync(); // hold until B commits
            }
            // retry: rr is already Received → deliberate no-op → 0 events
        }, CancellationToken.None);

        await barrier.WaitAsync(TimeSpan.FromSeconds(5));

        // B commits MarkAsReceived(v=1) while A is frozen at v=0
        await svcB.UpdateReturnRequestAsync(orderId.Value, rr =>
        {
            rr.MarkAsReceived();
            return Task.CompletedTask;
        }, CancellationToken.None);

        release.Release(); // A resumes => conflict => retry => no-op => success

        await taskA; // must not throw

        attemptCount.Should().Be(2, "first attempt conflicted, second attempt succeeded");

        await using var assertScope = _fixture.CreateScope();
        var assertSvc = assertScope.ServiceProvider.GetRequiredService<IReturnRequestPersistenceService>();
        var finalRr = await assertSvc.LoadByOrderIdAsync(orderId.Value, CancellationToken.None);

        finalRr!.Version.Should().Be(1, "only one MarkAsReceived event committed — by Task B");
    }
}

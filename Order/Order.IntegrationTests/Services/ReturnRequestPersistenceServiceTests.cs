using Application.Interfaces;
using Domain.Entities.Order;
using Domain.Entities.RequestReturn;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.DbContext;
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
    
    private static (RequestReturn Request, OrderId OrderId, CustomerId CustomerId) BuildReturnRequest()
    {
        var productId   = ProductId.CreateUnique();
        var orderItem   = OrderItem.Create(productId, 1, Money.Create(80, "USD"));
        var orderItems  = new List<OrderItem> { orderItem };
        var orderId     = OrderId.CreateUnique();
        var customerId  = CustomerId.CreateUnique();

        var request = RequestReturn.Create(
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
    

    [Fact]
    public async Task CreateReturnRequestAsync_ShouldSaveEventsAndOutboxMessage()
    {
        await using var scope = _fixture.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IReturnRequestPersistenceService>();
        var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (request, orderId, _) = BuildReturnRequest();
        
        await svc.CreateReturnRequestAsync(request, cancellationToken: CancellationToken.None);

        // Events are saved under the ReturnRequest's own aggregate ID (not the OrderId)
        var events = await db.DomainEvents
            .Where(e => e.AggregateId == request.Id.Value.ToString())
            .ToListAsync();

        var outbox = await db.OutboxMessages
            .Where(m => m.Id == events[0].EventId)
            .ToListAsync();

        // Lookup row must exist in the same transaction as the events
        var lookup = await db.ReturnRequestLookups
            .FirstOrDefaultAsync(l => l.OrderId == orderId.Value);

        events.Should().HaveCount(1, "only ReturnRequestCreatedEvent is raised on Create");
        outbox.Should().HaveCount(1, "outbox row must mirror every domain event");
        lookup.Should().NotBeNull("lookup must be written atomically with events");
        lookup!.ReturnRequestId.Should().Be(request.Id.Value);
    }
    
    [Fact]
    public async Task LoadByOrderIdAsync_ShouldFindReturnRequest_WithoutReadModel()
    {
        await using var scope = _fixture.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IReturnRequestPersistenceService>();

        var (request, orderId, _) = BuildReturnRequest();

        // CreateReturnRequestAsync writes the lookup in the same transaction — no read model needed
        await svc.CreateReturnRequestAsync(request, cancellationToken: CancellationToken.None);

        var loaded = await svc.LoadByOrderIdAsync(orderId.Value, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Id.Value.Should().Be(request.Id.Value,
            "lookup table must map orderId → returnRequestId without any read model dependency");
        loaded.OrderId.Value.Should().Be(orderId.Value);
        loaded.Reason.Should().Be(request.Reason);
    }

    [Fact]
    public async Task LoadByOrderIdAsync_ShouldReturnNull_WhenNoReturnRequestExists()
    {
        await using var scope = _fixture.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IReturnRequestPersistenceService>();

        var result = await svc.LoadByOrderIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull("no return request was ever created for this orderId");
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

        var (request, orderId, _) = BuildReturnRequest();
        await setupSvc.CreateReturnRequestAsync(request, cancellationToken: CancellationToken.None);
        // no read model seeding needed — lookup is written atomically in CreateReturnRequestAsync

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

        finalRr!.Version.Should().Be(2, "ReturnRequestCreated(v=0) + B's MarkAsReceived(v=1) = 2 committed events");
    }
}

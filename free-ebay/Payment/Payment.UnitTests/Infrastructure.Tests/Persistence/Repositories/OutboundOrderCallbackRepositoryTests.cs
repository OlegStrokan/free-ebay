using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Repositories;

namespace Infrastructure.Tests.Persistence.Repositories;

public class OutboundOrderCallbackRepositoryTests
{
    [Fact]
    public async Task GetByCallbackEventIdAsync_ShouldReturnMatch()
    {
        await using var context = Persistence.TestDbContextFactory.Create();
        var repository = new OutboundOrderCallbackRepository(context);

        var callback = OutboundOrderCallback.Create("evt-1", "order-1", "PaymentSucceededEvent", "{}", DateTime.UtcNow.AddMinutes(-5));
        await repository.AddAsync(callback);
        await context.SaveChangesAsync();

        var loaded = await repository.GetByCallbackEventIdAsync("evt-1");

        Assert.NotNull(loaded);
        Assert.Equal(callback.Id, loaded!.Id);
    }

    [Fact]
    public async Task GetPendingAsync_ShouldReturnPendingAndRetryableFailed()
    {
        await using var context = Persistence.TestDbContextFactory.Create();
        var repository = new OutboundOrderCallbackRepository(context);
        var now = DateTime.UtcNow;

        var pending = OutboundOrderCallback.Create("evt-pending", "order-1", "PaymentSucceededEvent", "{}", now.AddMinutes(-10));

        var failedRetryable = OutboundOrderCallback.Create("evt-failed", "order-2", "PaymentFailedEvent", "{}", now.AddMinutes(-9));
        failedRetryable.MarkAttemptFailed("HTTP 500", now.AddMinutes(-1), now.AddMinutes(-8));

        var delivered = OutboundOrderCallback.Create("evt-delivered", "order-3", "PaymentSucceededEvent", "{}", now.AddMinutes(-7));
        delivered.MarkDelivered(now.AddMinutes(-6));

        await repository.AddAsync(pending);
        await repository.AddAsync(failedRetryable);
        await repository.AddAsync(delivered);
        await context.SaveChangesAsync();

        var list = await repository.GetPendingAsync(now, 10);

        Assert.Equal(2, list.Count);
        Assert.Contains(list, x => x.CallbackEventId == "evt-pending");
        Assert.Contains(list, x => x.CallbackEventId == "evt-failed");
        Assert.DoesNotContain(list, x => x.Status == CallbackDeliveryStatus.Delivered);
    }
}

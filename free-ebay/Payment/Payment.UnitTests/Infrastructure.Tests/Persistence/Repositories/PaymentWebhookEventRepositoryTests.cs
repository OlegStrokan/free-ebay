using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Repositories;

namespace Infrastructure.Tests.Persistence.Repositories;

public class PaymentWebhookEventRepositoryTests
{
    [Fact]
    public async Task GetByProviderEventIdAsync_ShouldReturnMatch()
    {
        await using var context = Persistence.TestDbContextFactory.Create();
        var repository = new PaymentWebhookEventRepository(context);

        var webhookEvent = PaymentWebhookEvent.Create("evt-1", "payment_intent.succeeded", "{}", DateTime.UtcNow.AddMinutes(-2));
        await repository.AddAsync(webhookEvent);
        await context.SaveChangesAsync();

        var loaded = await repository.GetByProviderEventIdAsync("evt-1");

        Assert.NotNull(loaded);
        Assert.Equal("payment_intent.succeeded", loaded!.EventType);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistModifiedStatus()
    {
        await using var context = Persistence.TestDbContextFactory.Create();
        var repository = new PaymentWebhookEventRepository(context);

        var webhookEvent = PaymentWebhookEvent.Create("evt-2", "payment_intent.failed", "{}", DateTime.UtcNow.AddMinutes(-3));
        await repository.AddAsync(webhookEvent);
        await context.SaveChangesAsync();

        webhookEvent.MarkFailed("processing failed", DateTime.UtcNow);
        await repository.UpdateAsync(webhookEvent);
        await context.SaveChangesAsync();

        var loaded = await repository.GetByProviderEventIdAsync("evt-2");

        Assert.NotNull(loaded);
        Assert.Equal(WebhookProcessingStatus.Failed, loaded!.ProcessingStatus);
        Assert.Equal("processing failed", loaded.ProcessingError);
    }
}

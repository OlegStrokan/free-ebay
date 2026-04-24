using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Persistence.Repositories;

namespace Infrastructure.Tests.Persistence.Repositories;

public class RefundRepositoryTests
{
    private static PaymentId PaymentId1 => PaymentId.From("pay-1");

    [Fact]
    public async Task GetByIdAsync_ShouldReturnRefund_WhenExists()
    {
        await using var context = Persistence.TestDbContextFactory.Create();
        var repository = new RefundRepository(context);

        var refund = Refund.Create(PaymentId1, Money.Create(10m, "USD"), "reason", IdempotencyKey.From("idem-1"));

        await repository.AddAsync(refund);
        await context.SaveChangesAsync();

        var loaded = await repository.GetByIdAsync(refund.Id);

        Assert.NotNull(loaded);
        Assert.Equal(refund.Id.Value, loaded!.Id.Value);
    }

    [Fact]
    public async Task GetByProviderRefundIdAsync_ShouldReturnMatch()
    {
        await using var context = Persistence.TestDbContextFactory.Create();
        var repository = new RefundRepository(context);

        var refund = Refund.Create(PaymentId1, Money.Create(12m, "USD"), "reason", IdempotencyKey.From("idem-2"));
        refund.MarkPendingProviderConfirmation(ProviderRefundId.From("re_2"));

        await repository.AddAsync(refund);
        await context.SaveChangesAsync();

        var loaded = await repository.GetByProviderRefundIdAsync(ProviderRefundId.From("re_2"));

        Assert.NotNull(loaded);
        Assert.Equal(refund.Id.Value, loaded!.Id.Value);
    }

    [Fact]
    public async Task GetPendingByPaymentIdAsync_ShouldReturnLatestPending()
    {
        await using var context = Persistence.TestDbContextFactory.Create();
        var repository = new RefundRepository(context);

        var old = Refund.Create(PaymentId1, Money.Create(10m, "USD"), "r1", IdempotencyKey.From("idem-3"), DateTime.UtcNow.AddHours(-2));
        old.MarkPendingProviderConfirmation(ProviderRefundId.From("re_old"), DateTime.UtcNow.AddHours(-1));

        var latest = Refund.Create(PaymentId1, Money.Create(8m, "USD"), "r2", IdempotencyKey.From("idem-4"), DateTime.UtcNow.AddMinutes(-30));

        await repository.AddAsync(old);
        await repository.AddAsync(latest);
        await context.SaveChangesAsync();

        var loaded = await repository.GetPendingByPaymentIdAsync(PaymentId1);

        Assert.NotNull(loaded);
        Assert.Equal(latest.Id.Value, loaded!.Id.Value);
    }

    [Fact]
    public async Task GetByPaymentIdAndIdempotencyKeyAsync_ShouldReturnMatch()
    {
        await using var context = Persistence.TestDbContextFactory.Create();
        var repository = new RefundRepository(context);

        var refund = Refund.Create(PaymentId1, Money.Create(9m, "USD"), "r3", IdempotencyKey.From("idem-5"));

        await repository.AddAsync(refund);
        await context.SaveChangesAsync();

        var loaded = await repository.GetByPaymentIdAndIdempotencyKeyAsync(PaymentId1, IdempotencyKey.From("idem-5"));

        Assert.NotNull(loaded);
        Assert.Equal(refund.Id.Value, loaded!.Id.Value);
    }

    [Fact]
    public async Task GetPendingProviderConfirmationsOlderThanAsync_ShouldFilterByStatusAndThreshold()
    {
        await using var context = Persistence.TestDbContextFactory.Create();
        var repository = new RefundRepository(context);

        var oldPending = Refund.Create(PaymentId1, Money.Create(11m, "USD"), "r4", IdempotencyKey.From("idem-6"), DateTime.UtcNow.AddHours(-2));
        oldPending.MarkPendingProviderConfirmation(ProviderRefundId.From("re_old"), DateTime.UtcNow.AddHours(-1));

        var succeeded = Refund.Create(PaymentId1, Money.Create(12m, "USD"), "r5", IdempotencyKey.From("idem-7"), DateTime.UtcNow.AddHours(-2));
        succeeded.MarkSucceeded(ProviderRefundId.From("re_done"), DateTime.UtcNow.AddMinutes(-20));

        await repository.AddAsync(oldPending);
        await repository.AddAsync(succeeded);
        await context.SaveChangesAsync();

        var list = await repository.GetPendingProviderConfirmationsOlderThanAsync(DateTime.UtcNow.AddMinutes(-10), 10);

        Assert.Single(list);
        Assert.Equal(oldPending.Id.Value, list[0].Id.Value);
    }
}

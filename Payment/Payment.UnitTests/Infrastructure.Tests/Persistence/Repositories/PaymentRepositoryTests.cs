using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Persistence.Repositories;

namespace Infrastructure.Tests.Persistence.Repositories;

public class PaymentRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnPayment_WhenExists()
    {
        await using var context = Persistence.TestDbContextFactory.Create();
        var repository = new PaymentRepository(context);

        var payment = Payment.Create(
            PaymentId.From("pay-1"),
            "order-1",
            "customer-1",
            Money.Create(10m, "USD"),
            PaymentMethod.Card,
            IdempotencyKey.From("idem-1"));

        await repository.AddAsync(payment);
        await context.SaveChangesAsync();

        var loaded = await repository.GetByIdAsync(payment.Id);

        Assert.NotNull(loaded);
        Assert.Equal(payment.Id.Value, loaded!.Id.Value);
        Assert.Equal("order-1", loaded.OrderId);
    }

    [Fact]
    public async Task GetByProviderPaymentIntentIdAsync_ShouldReturnMatchedPayment()
    {
        await using var context = Persistence.TestDbContextFactory.Create();
        var repository = new PaymentRepository(context);

        var payment = Payment.Create(
            PaymentId.From("pay-2"),
            "order-2",
            "customer-2",
            Money.Create(20m, "USD"),
            PaymentMethod.Card,
            IdempotencyKey.From("idem-2"));

        payment.MarkPendingProviderConfirmation(ProviderPaymentIntentId.From("pi_2"));

        await repository.AddAsync(payment);
        await context.SaveChangesAsync();

        var loaded = await repository.GetByProviderPaymentIntentIdAsync(ProviderPaymentIntentId.From("pi_2"));

        Assert.NotNull(loaded);
        Assert.Equal(payment.Id.Value, loaded!.Id.Value);
    }

    [Fact]
    public async Task GetByOrderIdAndIdempotencyKeyAsync_ShouldReturnMatchedPayment()
    {
        await using var context = Persistence.TestDbContextFactory.Create();
        var repository = new PaymentRepository(context);

        var payment = Payment.Create(
            PaymentId.From("pay-3"),
            "order-3",
            "customer-3",
            Money.Create(30m, "USD"),
            PaymentMethod.Card,
            IdempotencyKey.From("idem-3"));

        await repository.AddAsync(payment);
        await context.SaveChangesAsync();

        var loaded = await repository.GetByOrderIdAndIdempotencyKeyAsync("order-3", IdempotencyKey.From("idem-3"));

        Assert.NotNull(loaded);
        Assert.Equal(payment.Id.Value, loaded!.Id.Value);
    }

    [Fact]
    public async Task GetPendingProviderConfirmationsOlderThanAsync_ShouldReturnOnlyPendingOlderThanThreshold()
    {
        await using var context = Persistence.TestDbContextFactory.Create();
        var repository = new PaymentRepository(context);

        var oldPending = Payment.Create(
            PaymentId.From("pay-4"),
            "order-4",
            "customer-4",
            Money.Create(40m, "USD"),
            PaymentMethod.Card,
            IdempotencyKey.From("idem-4"),
            DateTime.UtcNow.AddHours(-2));
        oldPending.MarkPendingProviderConfirmation(ProviderPaymentIntentId.From("pi_4"), DateTime.UtcNow.AddHours(-1));

        var succeeded = Payment.Create(
            PaymentId.From("pay-5"),
            "order-5",
            "customer-5",
            Money.Create(50m, "USD"),
            PaymentMethod.Card,
            IdempotencyKey.From("idem-5"),
            DateTime.UtcNow.AddHours(-2));
        succeeded.MarkSucceeded(ProviderPaymentIntentId.From("pi_5"), DateTime.UtcNow.AddHours(-1));

        await repository.AddAsync(oldPending);
        await repository.AddAsync(succeeded);
        await context.SaveChangesAsync();

        var list = await repository.GetPendingProviderConfirmationsOlderThanAsync(DateTime.UtcNow.AddMinutes(-10), 10);

        Assert.Single(list);
        Assert.Equal(oldPending.Id.Value, list[0].Id.Value);
    }
}

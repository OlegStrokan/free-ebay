using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Payment.IntegrationTests.Infrastructure;
using Xunit;

namespace Payment.IntegrationTests.Persistence;

[Collection("PaymentIntegration")]
public sealed class RepositoryIntegrationTests
{
    private readonly IntegrationFixture _fixture;

    public RepositoryIntegrationTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PaymentRepository_ShouldReadByOrderAndIdempotency()
    {
        await using var scope = _fixture.CreateScope();
        var paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<Application.Interfaces.IUnitOfWork>();

        var orderId = $"order-{Guid.NewGuid():N}";
        var key = IdempotencyKey.From($"idem-{Guid.NewGuid():N}");

        var payment = Domain.Entities.Payment.Create(
            PaymentId.From(Guid.NewGuid().ToString("N")),
            orderId,
            $"customer-{Guid.NewGuid():N}",
            Money.Create(70m, "USD"),
            PaymentMethod.Card,
            key,
            DateTime.UtcNow.AddMinutes(-30));

        payment.MarkPendingProviderConfirmation(ProviderPaymentIntentId.From($"pi_{Guid.NewGuid():N}"), DateTime.UtcNow.AddMinutes(-20));

        await paymentRepository.AddAsync(payment);
        await unitOfWork.SaveChangesAsync();

        var byOrder = await paymentRepository.GetByOrderIdAndIdempotencyKeyAsync(orderId, key);
        var pending = await paymentRepository.GetPendingProviderConfirmationsOlderThanAsync(DateTime.UtcNow.AddMinutes(-1), 100);

        Assert.NotNull(byOrder);
        Assert.Equal(payment.Id.Value, byOrder!.Id.Value);
        Assert.Contains(pending, x => x.Id == payment.Id);
    }

    [Fact]
    public async Task RefundRepository_ShouldReadLatestPendingByPaymentId()
    {
        await using var scope = _fixture.CreateScope();
        var paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var refundRepository = scope.ServiceProvider.GetRequiredService<IRefundRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<Application.Interfaces.IUnitOfWork>();

        var payment = Domain.Entities.Payment.Create(
            PaymentId.From(Guid.NewGuid().ToString("N")),
            $"order-{Guid.NewGuid():N}",
            $"customer-{Guid.NewGuid():N}",
            Money.Create(100m, "USD"),
            PaymentMethod.Card,
            IdempotencyKey.From($"idem-{Guid.NewGuid():N}"));
        payment.MarkSucceeded(ProviderPaymentIntentId.From($"pi_{Guid.NewGuid():N}"));

        await paymentRepository.AddAsync(payment);

        var old = Refund.Create(payment.Id, Money.Create(20m, "USD"), "old", IdempotencyKey.From($"ridem-{Guid.NewGuid():N}"), DateTime.UtcNow.AddHours(-2));
        old.MarkPendingProviderConfirmation(ProviderRefundId.From($"re_{Guid.NewGuid():N}"), DateTime.UtcNow.AddHours(-1));

        var latest = Refund.Create(payment.Id, Money.Create(10m, "USD"), "latest", IdempotencyKey.From($"ridem-{Guid.NewGuid():N}"));

        await refundRepository.AddAsync(old);
        await refundRepository.AddAsync(latest);
        await unitOfWork.SaveChangesAsync();

        var pending = await refundRepository.GetPendingByPaymentIdAsync(payment.Id);

        Assert.NotNull(pending);
        Assert.Equal(latest.Id.Value, pending!.Id.Value);
    }

    [Fact]
    public async Task CallbackAndWebhookRepositories_ShouldPersistAndQuery()
    {
        await using var scope = _fixture.CreateScope();
        var callbackRepository = scope.ServiceProvider.GetRequiredService<IOutboundOrderCallbackRepository>();
        var webhookRepository = scope.ServiceProvider.GetRequiredService<IPaymentWebhookEventRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<Application.Interfaces.IUnitOfWork>();

        var callback = OutboundOrderCallback.Create(
            callbackEventId: Guid.NewGuid().ToString("N"),
            orderId: $"order-{Guid.NewGuid():N}",
            eventType: "PaymentSucceededEvent",
            payloadJson: "{}",
            createdAt: DateTime.UtcNow.AddMinutes(-10));

        var webhook = PaymentWebhookEvent.Create(
            providerEventId: $"evt-{Guid.NewGuid():N}",
            eventType: "payment_intent.succeeded",
            payloadJson: "{}",
            receivedAt: DateTime.UtcNow.AddMinutes(-5));

        await callbackRepository.AddAsync(callback);
        await webhookRepository.AddAsync(webhook);
        await unitOfWork.SaveChangesAsync();

        var pendingCallbacks = await callbackRepository.GetPendingAsync(DateTime.UtcNow, 20);
        var loadedWebhook = await webhookRepository.GetByProviderEventIdAsync(webhook.ProviderEventId);

        Assert.Contains(pendingCallbacks, c => c.CallbackEventId == callback.CallbackEventId);
        Assert.NotNull(loadedWebhook);
        Assert.Equal(webhook.ProviderEventId, loadedWebhook!.ProviderEventId);
    }
}

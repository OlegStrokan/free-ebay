using Application.Commands.ReconcilePendingPayments;
using Application.DTOs;
using Application.Gateways;
using Application.Gateways.Models;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Application.Tests.Commands;

public class ReconcilePendingPaymentsCommandHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 3, 24, 18, 0, 0, DateTimeKind.Utc);

    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IRefundRepository _refundRepository = Substitute.For<IRefundRepository>();
    private readonly IStripePaymentProvider _stripePaymentProvider = Substitute.For<IStripePaymentProvider>();
    private readonly IOrderCallbackQueueService _queueService = Substitute.For<IOrderCallbackQueueService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ILogger<ReconcilePendingPaymentsCommandHandler> _logger = NullLogger<ReconcilePendingPaymentsCommandHandler>.Instance;

    public ReconcilePendingPaymentsCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    private ReconcilePendingPaymentsCommandHandler BuildHandler() =>
        new(_paymentRepository, _refundRepository, _stripePaymentProvider, _queueService, _unitOfWork, _clock, _logger);

    private static Payment CreatePendingPayment()
    {
        var payment = Payment.Create(
            PaymentId.From("pay-1"),
            "order-1",
            "customer-1",
            Money.Create(100m, "USD"),
            PaymentMethod.Card,
            IdempotencyKey.From("idem-1"),
            FixedNow.AddMinutes(-30));

        payment.MarkPendingProviderConfirmation(ProviderPaymentIntentId.From("pi_1"), FixedNow.AddMinutes(-29));
        return payment;
    }

    private static Refund CreatePendingRefund(Payment payment)
    {
        var refund = Refund.Create(
            payment.Id,
            Money.Create(30m, "USD"),
            "customer request",
            IdempotencyKey.From("refund-idem"),
            FixedNow.AddMinutes(-20));

        refund.MarkPendingProviderConfirmation(ProviderRefundId.From("re_1"), FixedNow.AddMinutes(-19));
        return refund;
    }

    [Fact]
    public async Task Handle_ShouldReconcilePaymentAndRefundAndQueueCallbacks()
    {
        var payment = CreatePendingPayment();
        var refund = CreatePendingRefund(payment);

        _paymentRepository.GetPendingProviderConfirmationsOlderThanAsync(
                Arg.Any<DateTime>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([payment]);

        _refundRepository.GetPendingProviderConfirmationsOlderThanAsync(
                Arg.Any<DateTime>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([refund]);

        _paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        _stripePaymentProvider.GetPaymentStatusAsync("pi_1", Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentStatusResult(ProviderPaymentLifecycleStatus.Succeeded, null, null));

        _stripePaymentProvider.GetRefundStatusAsync("re_1", Arg.Any<CancellationToken>())
            .Returns(new ProviderRefundStatusResult(ProviderRefundLifecycleStatus.Failed, "RF_FAIL", "provider fail"));

        var result = await BuildHandler().Handle(new ReconcilePendingPaymentsCommand(15, 100), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value!.PaymentsChecked);
        Assert.Equal(1, result.Value.PaymentsSucceeded);
        Assert.Equal(1, result.Value.RefundsChecked);
        Assert.Equal(1, result.Value.RefundsFailed);
        Assert.Equal(2, result.Value.CallbacksQueued);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUnexpectedExceptionOccurs()
    {
        _paymentRepository.GetPendingProviderConfirmationsOlderThanAsync(
                Arg.Any<DateTime>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<Payment>>>(_ => throw new InvalidOperationException("boom"));

        var result = await BuildHandler().Handle(new ReconcilePendingPaymentsCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Unexpected error", result.Errors[0]);
    }
}

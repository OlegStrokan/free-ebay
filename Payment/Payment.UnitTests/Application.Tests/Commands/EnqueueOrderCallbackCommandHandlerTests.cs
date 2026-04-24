using Application.Commands.EnqueueOrderCallback;
using Application.Common;
using Application.DTOs;
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

public class EnqueueOrderCallbackCommandHandlerTests
{
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IRefundRepository _refundRepository = Substitute.For<IRefundRepository>();
    private readonly IOrderCallbackQueueService _queueService = Substitute.For<IOrderCallbackQueueService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<EnqueueOrderCallbackCommandHandler> _logger = NullLogger<EnqueueOrderCallbackCommandHandler>.Instance;

    private EnqueueOrderCallbackCommandHandler BuildHandler() =>
        new(_paymentRepository, _refundRepository, _queueService, _unitOfWork, _logger);

    private static Payment CreateFailedPayment()
    {
        var payment = Payment.Create(
            PaymentId.From("pay-1"),
            "order-1",
            "customer-1",
            Money.Create(100m, "USD"),
            PaymentMethod.Card,
            IdempotencyKey.From("idem-1"));

        payment.MarkFailed(FailureReason.Create("DECLINED", "Bank declined"));
        return payment;
    }

    private static Refund CreatePendingRefund(Payment payment)
    {
        var refund = Refund.Create(
            payment.Id,
            Money.Create(50m, "USD"),
            "requested_by_customer",
            IdempotencyKey.From("refund-idem"));

        refund.MarkPendingProviderConfirmation(ProviderRefundId.From("re_1"));
        return refund;
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPaymentNotFound()
    {
        _paymentRepository.GetByIdAsync(Arg.Any<PaymentId>(), Arg.Any<CancellationToken>())
            .Returns((Payment?)null);

        var command = new EnqueueOrderCallbackCommand("missing", OrderCallbackType.PaymentFailed, null, null, null);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("was not found", result.Errors[0]);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRefundCallbackAndRefundMissing()
    {
        var payment = CreateFailedPayment();

        _paymentRepository.GetByIdAsync(Arg.Any<PaymentId>(), Arg.Any<CancellationToken>())
            .Returns(payment);
        _refundRepository.GetByIdAsync(Arg.Any<RefundId>(), Arg.Any<CancellationToken>())
            .Returns((Refund?)null);

        var command = new EnqueueOrderCallbackCommand(
            "pay-1",
            OrderCallbackType.RefundSucceeded,
            "missing-refund",
            null,
            null);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Refund", result.Errors[0]);

        await _queueService.DidNotReceive().QueueRefundSucceededAsync(
            Arg.Any<Payment>(),
            Arg.Any<Refund>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldQueueRefundSucceeded_AndPersist_WhenDataValid()
    {
        var payment = CreateFailedPayment();
        var refund = CreatePendingRefund(payment);

        _paymentRepository.GetByIdAsync(Arg.Any<PaymentId>(), Arg.Any<CancellationToken>())
            .Returns(payment);
        _refundRepository.GetByIdAsync(Arg.Any<RefundId>(), Arg.Any<CancellationToken>())
            .Returns(refund);

        _queueService
            .QueueRefundSucceededAsync(Arg.Any<Payment>(), Arg.Any<Refund>(), Arg.Any<CancellationToken>())
            .Returns(new OrderCallbackQueuedDto("evt-1", "pay-1", OrderCallbackEventTypes.RefundSucceeded, "order-1", DateTime.UtcNow));

        var command = new EnqueueOrderCallbackCommand(
            "pay-1",
            OrderCallbackType.RefundSucceeded,
            refund.Id.Value,
            null,
            null);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("evt-1", result.Value!.CallbackEventId);

        await _paymentRepository.Received(1).UpdateAsync(payment, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

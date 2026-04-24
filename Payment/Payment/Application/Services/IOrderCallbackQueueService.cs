using Application.DTOs;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Services;

public interface IOrderCallbackQueueService
{
    Task<OrderCallbackQueuedDto> QueuePaymentSucceededAsync(Payment payment, CancellationToken cancellationToken = default);

    Task<OrderCallbackQueuedDto> QueuePaymentFailedAsync(
        Payment payment,
        FailureReason reason,
        CancellationToken cancellationToken = default);

    Task<OrderCallbackQueuedDto> QueueRefundSucceededAsync(
        Payment payment,
        Refund refund,
        CancellationToken cancellationToken = default);

    Task<OrderCallbackQueuedDto> QueueRefundFailedAsync(
        Payment payment,
        Refund refund,
        FailureReason reason,
        CancellationToken cancellationToken = default);
}
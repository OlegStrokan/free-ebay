using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.EnqueueOrderCallback;

// should be used only for manual investigation of failed payments. probably will be exposed as rest api for admin
// It only enqueues callback events to outbox for delivery
internal sealed class EnqueueOrderCallbackCommandHandler(
    IPaymentRepository paymentRepository,
    IRefundRepository refundRepository,
    IOrderCallbackQueueService orderCallbackQueueService,
    IUnitOfWork unitOfWork,
    ILogger<EnqueueOrderCallbackCommandHandler> logger)
    : IRequestHandler<EnqueueOrderCallbackCommand, Result<OrderCallbackQueuedDto>>
{
    public async Task<Result<OrderCallbackQueuedDto>> Handle(
        EnqueueOrderCallbackCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var paymentId = PaymentId.From(request.PaymentId);
            var payment = await paymentRepository.GetByIdAsync(paymentId, cancellationToken);

            if (payment is null)
            {
                return Result<OrderCallbackQueuedDto>.Failure($"Payment '{request.PaymentId}' was not found");
            }

            OrderCallbackQueuedDto callback;

            switch (request.CallbackType)
            {
                case OrderCallbackType.PaymentSucceeded:
                    callback = await orderCallbackQueueService.QueuePaymentSucceededAsync(payment, cancellationToken);
                    break;

                case OrderCallbackType.PaymentFailed:
                {
                    var reason = FailureReason.Create(
                        request.ErrorCode,
                        request.ErrorMessage
                        ?? payment.FailureReason?.Message
                        ?? "Payment failed callback requested manually");

                    callback = await orderCallbackQueueService.QueuePaymentFailedAsync(payment, reason, cancellationToken);
                    break;
                }

                case OrderCallbackType.RefundSucceeded:
                {
                    var refund = await LoadRefundAsync(request.RefundId, cancellationToken);
                    if (refund is null)
                    {
                        return Result<OrderCallbackQueuedDto>.Failure(
                            $"Refund '{request.RefundId}' was not found");
                    }

                    callback = await orderCallbackQueueService.QueueRefundSucceededAsync(payment, refund, cancellationToken);
                    break;
                }

                case OrderCallbackType.RefundFailed:
                {
                    var refund = await LoadRefundAsync(request.RefundId, cancellationToken);
                    if (refund is null)
                    {
                        return Result<OrderCallbackQueuedDto>.Failure(
                            $"Refund '{request.RefundId}' was not found");
                    }

                    var reason = FailureReason.Create(
                        request.ErrorCode,
                        request.ErrorMessage
                        ?? refund.FailureReason?.Message
                        ?? "Refund failed callback requested manually");

                    callback = await orderCallbackQueueService.QueueRefundFailedAsync(
                        payment,
                        refund,
                        reason,
                        cancellationToken);
                    break;
                }

                default:
                    return Result<OrderCallbackQueuedDto>.Failure("Unsupported callback type");
            }

            await paymentRepository.UpdateAsync(payment, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<OrderCallbackQueuedDto>.Success(callback);
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "EnqueueOrderCallback domain validation failed");
            return Result<OrderCallbackQueuedDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EnqueueOrderCallback failed unexpectedly");
            return Result<OrderCallbackQueuedDto>.Failure("Unexpected error while queueing order callback");
        }
    }

    private async Task<Domain.Entities.Refund?> LoadRefundAsync(string? refundId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refundId))
        {
            return null;
        }

        return await refundRepository.GetByIdAsync(RefundId.From(refundId), cancellationToken);
    }
}
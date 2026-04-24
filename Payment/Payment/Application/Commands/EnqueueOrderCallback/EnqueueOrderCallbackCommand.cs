using Application.Common;
using Application.DTOs;

namespace Application.Commands.EnqueueOrderCallback;

public sealed record EnqueueOrderCallbackCommand(
    string PaymentId,
    OrderCallbackType CallbackType,
    string? RefundId,
    string? ErrorCode,
    string? ErrorMessage) : ICommand<Result<OrderCallbackQueuedDto>>;
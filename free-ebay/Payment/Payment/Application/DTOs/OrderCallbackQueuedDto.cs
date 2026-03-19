namespace Application.DTOs;

public sealed record OrderCallbackQueuedDto(
    string CallbackEventId,
    string PaymentId,
    string CallbackType,
    string OrderId,
    DateTime QueuedAt);
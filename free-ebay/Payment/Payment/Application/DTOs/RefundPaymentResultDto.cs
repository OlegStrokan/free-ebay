namespace Application.DTOs;

public sealed record RefundPaymentResultDto(
    string PaymentId,
    string RefundId,
    RefundPaymentStatus Status,
    string? ProviderRefundId,
    string? ErrorCode,
    string? ErrorMessage);
namespace Application.Gateways.Models;

public sealed record RefundPaymentProviderResult(
    ProviderRefundPaymentStatus Status,
    string? ProviderRefundId,
    string? ErrorCode,
    string? ErrorMessage);
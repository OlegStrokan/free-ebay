namespace Application.Gateways.Models;

public sealed record RefundPaymentProviderRequest(
    string PaymentId,
    string? ProviderPaymentIntentId,
    decimal Amount,
    string Currency,
    string Reason,
    string IdempotencyKey);
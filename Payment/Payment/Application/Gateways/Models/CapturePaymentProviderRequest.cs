namespace Application.Gateways.Models;

public sealed record CapturePaymentProviderRequest(
    string PaymentId,
    string OrderId,
    string CustomerId,
    string ProviderPaymentIntentId,
    decimal Amount,
    string Currency,
    string IdempotencyKey);

namespace Application.Gateways.Models;

public sealed record CapturePaymentProviderResult(
    ProviderProcessPaymentStatus Status,
    string? ProviderPaymentIntentId,
    string? ErrorCode,
    string? ErrorMessage);

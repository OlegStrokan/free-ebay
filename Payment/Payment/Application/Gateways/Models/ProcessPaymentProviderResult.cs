namespace Application.Gateways.Models;

public sealed record ProcessPaymentProviderResult(
    ProviderProcessPaymentStatus Status,
    string? ProviderPaymentIntentId,
    string? ClientSecret,
    string? ErrorCode,
    string? ErrorMessage);
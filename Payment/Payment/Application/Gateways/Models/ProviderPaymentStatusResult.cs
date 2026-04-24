namespace Application.Gateways.Models;

public sealed record ProviderPaymentStatusResult(
    ProviderPaymentLifecycleStatus Status,
    string? ErrorCode,
    string? ErrorMessage);
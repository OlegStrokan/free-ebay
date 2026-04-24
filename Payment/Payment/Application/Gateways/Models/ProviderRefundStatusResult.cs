namespace Application.Gateways.Models;

public sealed record ProviderRefundStatusResult(
    ProviderRefundLifecycleStatus Status,
    string? ErrorCode,
    string? ErrorMessage);
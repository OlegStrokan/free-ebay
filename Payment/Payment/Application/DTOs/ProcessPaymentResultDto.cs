namespace Application.DTOs;

public sealed record ProcessPaymentResultDto(
    string PaymentId,
    ProcessPaymentStatus Status,
    string? ProviderPaymentIntentId,
    string? ClientSecret,
    string? ErrorCode,
    string? ErrorMessage);
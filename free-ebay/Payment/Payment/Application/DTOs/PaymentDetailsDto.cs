using Domain.Enums;

namespace Application.DTOs;

public sealed record PaymentDetailsDto(
    string PaymentId,
    string OrderId,
    string CustomerId,
    decimal Amount,
    string Currency,
    PaymentMethod PaymentMethod,
    PaymentStatus Status,
    string? ProviderPaymentIntentId,
    string? ProviderRefundId,
    string? FailureCode,
    string? FailureMessage,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SucceededAt,
    DateTime? FailedAt);
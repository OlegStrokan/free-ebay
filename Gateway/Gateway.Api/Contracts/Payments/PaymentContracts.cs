namespace Gateway.Api.Contracts.Payments;

public sealed record PaymentDetailsResponse(
    string PaymentId,
    string OrderId,
    string CustomerId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string Status,
    string? ProviderPaymentIntentId,
    string? ProviderRefundId,
    string? FailureCode,
    string? FailureMessage,
    long CreatedAtUnix,
    long UpdatedAtUnix,
    long SucceededAtUnix,
    long FailedAtUnix);

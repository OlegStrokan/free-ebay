namespace Application.DTOs;

public sealed record PaymentFailedEventDto
{
    public string OrderId { get; init; } = string.Empty;
    public string PaymentId { get; init; } = string.Empty;
    public string? ProviderPaymentIntentId { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? CallbackEventId { get; init; }
    public DateTime? OccurredOn { get; init; }
}

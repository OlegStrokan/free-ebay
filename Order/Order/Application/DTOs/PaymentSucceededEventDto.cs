namespace Application.DTOs;

public sealed record PaymentSucceededEventDto
{
    public string OrderId { get; init; } = string.Empty;
    public string PaymentId { get; init; } = string.Empty;
    public string? ProviderPaymentIntentId { get; init; }
    public string? CallbackEventId { get; init; }
    public DateTime? OccurredOn { get; init; }
}

namespace Application.DTOs;

public sealed record WebhookProcessingResultDto(
    bool IsDuplicate,
    bool Processed,
    bool IsIgnored,
    string ProviderEventId,
    string EventType,
    string? PaymentId,
    string? Error);
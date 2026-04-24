namespace Domain.Enums;

public enum WebhookProcessingStatus
{
    Received = 0,
    Processed = 1,
    Failed = 2,
    IgnoredDuplicate = 3,
}
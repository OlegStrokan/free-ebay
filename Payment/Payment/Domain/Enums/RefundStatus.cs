namespace Domain.Enums;

public enum RefundStatus
{
    Requested = 0,
    PendingProviderConfirmation = 1,
    Succeeded = 2,
    Failed = 3,
}
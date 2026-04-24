namespace Application.DTOs;

public sealed record ReconciliationResultDto(
    int PaymentsChecked,
    int PaymentsSucceeded,
    int PaymentsFailed,
    int RefundsChecked,
    int RefundsSucceeded,
    int RefundsFailed,
    int CallbacksQueued);
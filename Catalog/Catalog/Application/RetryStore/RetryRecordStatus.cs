namespace Application.RetryStore;

public enum RetryRecordStatus
{
    Pending,
    InProgress,
    Succeeded,
    DeadLetter,
}

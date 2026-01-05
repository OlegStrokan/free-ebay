namespace Domain.ValueObjects;

public enum OrderStatus
{
    Pending = 1,
    AwaitingPayment = 2,
    Paid = 3,
    Approved = 4,
    Cancelling = 5,
    Cancelled = 6,
    Completed = 7
}
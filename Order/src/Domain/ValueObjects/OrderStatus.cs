namespace Domain.ValueObjects;

public enum OrderStatus
{
    Pending = 0,
    AwaitingPayment = 1,
    Paid = 2,
    Approved = 3,
    Cancelling = 4,
    Cancelled = 5,
    Completed = 6,
    
    //return/refund
    ReturnRequested = 7,
    ReturnReceived = 8,
    Refunded = 9,
    Returned = 10
}
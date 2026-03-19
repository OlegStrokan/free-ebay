namespace Application.Commands.EnqueueOrderCallback;

public enum OrderCallbackType
{
    PaymentSucceeded = 0,
    PaymentFailed = 1,
    RefundSucceeded = 2,
    RefundFailed = 3,
}
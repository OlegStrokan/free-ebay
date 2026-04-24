namespace Application.Common;

public static class OrderCallbackEventTypes
{
    public const string PaymentSucceeded = "PaymentSucceededEvent";

    public const string PaymentFailed = "PaymentFailedEvent";

    public const string RefundSucceeded = "RefundSucceededEvent";

    public const string RefundFailed = "RefundFailedEvent";
}
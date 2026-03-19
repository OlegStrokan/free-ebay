namespace Application.Commands.HandleStripeWebhook;

public enum StripeWebhookOutcome
{
    Unknown = 0,
    PaymentSucceeded = 1,
    PaymentFailed = 2,
    RefundSucceeded = 3,
    RefundFailed = 4,
}
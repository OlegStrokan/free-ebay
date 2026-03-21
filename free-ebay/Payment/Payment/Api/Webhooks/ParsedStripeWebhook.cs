using Application.Commands.HandleStripeWebhook;

namespace Api.Webhooks;

internal sealed record ParsedStripeWebhook(
    string ProviderEventId,
    string EventType,
    StripeWebhookOutcome Outcome,
    string? PaymentId,
    string? ProviderPaymentIntentId,
    string? ProviderRefundId,
    string? FailureCode,
    string? FailureMessage);
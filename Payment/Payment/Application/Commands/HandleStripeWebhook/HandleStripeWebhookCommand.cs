using Application.Common;
using Application.DTOs;

namespace Application.Commands.HandleStripeWebhook;

public sealed record HandleStripeWebhookCommand(
    string ProviderEventId,
    string EventType,
    string PayloadJson,
    StripeWebhookOutcome Outcome,
    string? PaymentId,
    string? ProviderPaymentIntentId,
    string? ProviderRefundId,
    string? FailureCode,
    string? FailureMessage) : ICommand<Result<WebhookProcessingResultDto>>;
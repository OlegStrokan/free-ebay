using Api.Webhooks;
using Application.Commands.HandleStripeWebhook;
using Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.Options;
using System.Text;

namespace Api.Endpoints;

public static class StripeWebhookEndpoint
{
    public static IEndpointRouteBuilder MapStripeWebhookEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/webhooks/stripe", HandleAsync);
        return endpoints;
    }

    public static async Task<IResult> HandleAsync(
        HttpRequest request,
        IMediator mediator,
        IOptions<StripeOptions> stripeOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("StripeWebhookEndpoint");

        string payloadJson;
        using (var reader = new StreamReader(request.Body, Encoding.UTF8))
        {
            payloadJson = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Results.BadRequest(new { Error = "Webhook payload cannot be empty" });
        }

        if (!StripeWebhookSignatureVerifier.TryValidate(
                request.Headers["Stripe-Signature"].ToString(),
                payloadJson,
                stripeOptions.Value,
                out var signatureError))
        {
            logger.LogWarning(
                "Stripe webhook signature validation failed. Error={Error}",
                signatureError ?? "unknown");
            return Results.Unauthorized();
        }

        if (!StripeWebhookParser.TryParse(payloadJson, out var parsedWebhook, out var parseError))
        {
            logger.LogWarning(
                "Failed to parse Stripe webhook payload. Error={Error}",
                parseError ?? "unknown error");
            return Results.BadRequest(new { Error = parseError ?? "Invalid Stripe webhook payload" });
        }

        var outcome = parsedWebhook.Outcome;
        if (outcome != StripeWebhookOutcome.Unknown
            && string.IsNullOrWhiteSpace(parsedWebhook.PaymentId)
            && string.IsNullOrWhiteSpace(parsedWebhook.ProviderPaymentIntentId)
            && string.IsNullOrWhiteSpace(parsedWebhook.ProviderRefundId))
        {
            outcome = StripeWebhookOutcome.Unknown;
        }

        var command = new HandleStripeWebhookCommand(
            ProviderEventId: parsedWebhook.ProviderEventId,
            EventType: parsedWebhook.EventType,
            PayloadJson: payloadJson,
            Outcome: outcome,
            PaymentId: parsedWebhook.PaymentId,
            ProviderPaymentIntentId: parsedWebhook.ProviderPaymentIntentId,
            ProviderRefundId: parsedWebhook.ProviderRefundId,
            FailureCode: parsedWebhook.FailureCode,
            FailureMessage: parsedWebhook.FailureMessage);

        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            var error = result.Errors.Count == 0
                ? "Webhook processing failed"
                : string.Join("; ", result.Errors);

            logger.LogWarning(
                "Stripe webhook processing failed. ProviderEventId={ProviderEventId}, Error={Error}",
                parsedWebhook.ProviderEventId,
                error);

            return Results.Problem(detail: error, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Results.Ok(result.Value);
    }
}
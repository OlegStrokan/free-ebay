using Application.Commands.EnqueueOrderCallback;
using Application.Common;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Api.Endpoints;

public static class AdminOrderCallbackEndpoint
{
    private const string ApiKeyHeader = "X-Admin-Key";

    public static IEndpointRouteBuilder MapAdminOrderCallbackEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/internal/admin/order-callbacks/enqueue", HandleAsync);
        return endpoints;
    }

    public static async Task<IResult> HandleAsync(
        EnqueueOrderCallbackHttpRequest request,
        HttpContext httpContext,
        IConfiguration configuration,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var expectedKey = configuration["Admin:ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            return Results.StatusCode(503);
        }

        var providedKey = httpContext.Request.Headers[ApiKeyHeader].FirstOrDefault();
        if (!string.Equals(providedKey, expectedKey, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }

        if (!TryParseCallbackType(request.CallbackType, out var callbackType))
        {
            return Results.BadRequest(new
            {
                Error = "Unsupported callback type. Use PaymentSucceeded, PaymentFailed, RefundSucceeded, or RefundFailed.",
            });
        }

        var command = new EnqueueOrderCallbackCommand(
            PaymentId: request.PaymentId,
            CallbackType: callbackType,
            RefundId: request.RefundId,
            ErrorCode: request.ErrorCode,
            ErrorMessage: request.ErrorMessage);

        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return Results.BadRequest(new
            {
                Errors = result.Errors,
            });
        }

        return Results.Ok(result.Value);
    }

    private static bool TryParseCallbackType(string? rawValue, out OrderCallbackType callbackType)
    {
        callbackType = default;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        if (Enum.TryParse<OrderCallbackType>(rawValue, ignoreCase: true, out callbackType)
            && Enum.IsDefined(callbackType))
        {
            return true;
        }

        callbackType = rawValue.Trim().ToLowerInvariant() switch
        {
            "payment-succeeded" => OrderCallbackType.PaymentSucceeded,
            "payment_succeeded" => OrderCallbackType.PaymentSucceeded,
            "payment-failed" => OrderCallbackType.PaymentFailed,
            "payment_failed" => OrderCallbackType.PaymentFailed,
            "refund-succeeded" => OrderCallbackType.RefundSucceeded,
            "refund_succeeded" => OrderCallbackType.RefundSucceeded,
            "refund-failed" => OrderCallbackType.RefundFailed,
            "refund_failed" => OrderCallbackType.RefundFailed,
            _ => (OrderCallbackType)(-1),
        };

        return callbackType is OrderCallbackType.PaymentSucceeded
                   or OrderCallbackType.PaymentFailed
                   or OrderCallbackType.RefundSucceeded
                   or OrderCallbackType.RefundFailed;
    }

    public sealed record EnqueueOrderCallbackHttpRequest(
        string PaymentId,
        string CallbackType,
        string? RefundId,
        string? ErrorCode,
        string? ErrorMessage);
}
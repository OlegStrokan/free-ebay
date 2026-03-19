using Domain.Entities;
using Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Callbacks;

internal sealed class OrderCallbackHttpDispatcher(
    HttpClient httpClient,
    IOptions<OrderCallbackOptions> options,
    ILogger<OrderCallbackHttpDispatcher> logger) : IOrderCallbackDispatcher
{
    private readonly OrderCallbackOptions _options = options.Value;

    public async Task<CallbackDeliveryResult> DispatchAsync(
        OutboundOrderCallback callback,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.EndpointUrl))
        {
            return new CallbackDeliveryResult(false, "Order callback endpoint URL is not configured.");
        }

        if (!Uri.TryCreate(_options.EndpointUrl, UriKind.Absolute, out var endpointUri))
        {
            return new CallbackDeliveryResult(false, "Order callback endpoint URL is not a valid absolute URI.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpointUri)
        {
            Content = new StringContent(callback.PayloadJson, Encoding.UTF8, "application/json"),
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("X-Callback-Event-Id", callback.CallbackEventId);
        request.Headers.TryAddWithoutValidation("X-Callback-Event-Type", callback.EventType);

        var signatureHeader = BuildSignatureHeader(callback.PayloadJson);
        if (!string.IsNullOrWhiteSpace(signatureHeader))
        {
            request.Headers.TryAddWithoutValidation("X-Payment-Signature", signatureHeader);
        }

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new CallbackDeliveryResult(true, null);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var bodySnippet = body.Length <= 512 ? body : body[..512];
            var error =
                $"Order callback endpoint returned {(int)response.StatusCode} {response.ReasonPhrase}. " +
                $"Response: {bodySnippet}";

            logger.LogWarning(
                "Outbound callback delivery failed. CallbackEventId={CallbackEventId}, StatusCode={StatusCode}",
                callback.CallbackEventId,
                (int)response.StatusCode);

            return new CallbackDeliveryResult(false, error);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Outbound callback delivery exception. CallbackEventId={CallbackEventId}",
                callback.CallbackEventId);

            return new CallbackDeliveryResult(false, ex.Message);
        }
    }

    private string? BuildSignatureHeader(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(_options.SharedSecret))
        {
            return null;
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var signedPayload = $"{timestamp}.{payloadJson}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SharedSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var signature = Convert.ToHexString(hash).ToLowerInvariant();

        return $"t={timestamp},v1={signature}";
    }
}
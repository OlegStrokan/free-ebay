namespace Infrastructure.Gateways;

public sealed class ShippingApiOptions
{
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 20;
    public string WebhookCallUrl { get; init; } = string.Empty;
}
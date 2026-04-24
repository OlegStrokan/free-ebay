namespace Infrastructure.Gateways.Carrier;

public sealed class DpdApiOptions
{
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 20;
}

namespace Infrastructure.Options;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public bool UseFakeProvider { get; init; } = true;

    public string SecretKey { get; init; } = string.Empty;

    public string WebhookSecret { get; init; } = string.Empty;

    public int WebhookToleranceSeconds { get; init; } = 300;

    public string DefaultCurrency { get; init; } = "USD";
}
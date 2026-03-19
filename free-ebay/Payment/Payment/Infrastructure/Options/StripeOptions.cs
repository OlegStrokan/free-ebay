namespace Infrastructure.Options;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public bool UseFakeProvider { get; init; } = true;

    public string SecretKey { get; init; } = string.Empty;

    public string DefaultCurrency { get; init; } = "USD";
}
namespace Infrastructure.Callbacks;

public sealed record CallbackDeliveryResult(bool Succeeded, string? Error);
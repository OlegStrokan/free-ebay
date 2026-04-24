using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Configurations;

internal static class ValueObjectConverters
{
    internal static readonly ValueConverter<PaymentId, string> PaymentId =
        new(v => v.Value, v => Domain.ValueObjects.PaymentId.From(v));

    internal static readonly ValueConverter<RefundId, string> RefundId =
        new(v => v.Value, v => Domain.ValueObjects.RefundId.From(v));

    internal static readonly ValueConverter<IdempotencyKey, string> IdempotencyKey =
        new(v => v.Value, v => Domain.ValueObjects.IdempotencyKey.From(v));

    internal static readonly ValueConverter<ProviderPaymentIntentId?, string?> NullableProviderPaymentIntentId =
        new(
            v => v == null ? null : v.Value,
            v => v == null ? null : Domain.ValueObjects.ProviderPaymentIntentId.From(v));

    internal static readonly ValueConverter<ProviderRefundId?, string?> NullableProviderRefundId =
        new(
            v => v == null ? null : v.Value,
            v => v == null ? null : Domain.ValueObjects.ProviderRefundId.From(v));
}
using Domain.ValueObjects;
using Infrastructure.Persistence.Configurations;

namespace Infrastructure.Tests.Persistence.Configurations;

public class ValueObjectConvertersTests
{
    [Fact]
    public void PaymentIdConverter_ShouldRoundTrip()
    {
        var id = PaymentId.From("pay-1");

        var toProvider = ValueObjectConverters.PaymentId.ConvertToProviderExpression.Compile();
        var fromProvider = ValueObjectConverters.PaymentId.ConvertFromProviderExpression.Compile();

        var raw = toProvider(id);
        var restored = fromProvider(raw);

        Assert.Equal("pay-1", raw);
        Assert.Equal(id, restored);
    }

    [Fact]
    public void RefundIdConverter_ShouldRoundTrip()
    {
        var id = RefundId.From("ref-1");
        var toProvider = ValueObjectConverters.RefundId.ConvertToProviderExpression.Compile();
        var fromProvider = ValueObjectConverters.RefundId.ConvertFromProviderExpression.Compile();

        var raw = toProvider(id);
        var restored = fromProvider(raw);

        Assert.Equal("ref-1", raw);
        Assert.Equal(id, restored);
    }

    [Fact]
    public void IdempotencyKeyConverter_ShouldRoundTrip()
    {
        var key = IdempotencyKey.From("idem-1");
        var toProvider = ValueObjectConverters.IdempotencyKey.ConvertToProviderExpression.Compile();
        var fromProvider = ValueObjectConverters.IdempotencyKey.ConvertFromProviderExpression.Compile();

        var raw = toProvider(key);
        var restored = fromProvider(raw);

        Assert.Equal("idem-1", raw);
        Assert.Equal(key, restored);
    }

    [Fact]
    public void NullableProviderPaymentIntentIdConverter_ShouldHandleNullAndValue()
    {
        var toProvider = ValueObjectConverters.NullableProviderPaymentIntentId.ConvertToProviderExpression.Compile();
        var fromProvider = ValueObjectConverters.NullableProviderPaymentIntentId.ConvertFromProviderExpression.Compile();

        Assert.Null(toProvider(null));
        Assert.Null(fromProvider(null));

        var value = ProviderPaymentIntentId.From("pi_1");
        var raw = toProvider(value);
        var restored = fromProvider(raw);

        Assert.Equal("pi_1", raw);
        Assert.Equal(value, restored);
    }

    [Fact]
    public void NullableProviderRefundIdConverter_ShouldHandleNullAndValue()
    {
        var toProvider = ValueObjectConverters.NullableProviderRefundId.ConvertToProviderExpression.Compile();
        var fromProvider = ValueObjectConverters.NullableProviderRefundId.ConvertFromProviderExpression.Compile();

        Assert.Null(toProvider(null));
        Assert.Null(fromProvider(null));

        var value = ProviderRefundId.From("re_1");
        var raw = toProvider(value);
        var restored = fromProvider(raw);

        Assert.Equal("re_1", raw);
        Assert.Equal(value, restored);
    }
}

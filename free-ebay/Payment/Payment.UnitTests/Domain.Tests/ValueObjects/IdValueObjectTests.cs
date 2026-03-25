using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class PaymentIdTests
{
    [Fact]
    public void From_ShouldCreateInstanceWithProvidedValue()
    {
        var id = PaymentId.From("pi_abc123");

        Assert.Equal("pi_abc123", id.Value);
    }

    [Fact]
    public void From_ShouldTrimWhitespace()
    {
        var id = PaymentId.From("  pi_abc123  ");

        Assert.Equal("pi_abc123", id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void From_WithEmptyValue_ShouldThrowInvalidValueException(string value)
    {
        var ex = Assert.Throws<InvalidValueException>(() => PaymentId.From(value));

        Assert.Contains("PaymentId cannot be empty", ex.Message);
    }

    [Fact]
    public void CreateUnique_ShouldReturnNonEmptyValue()
    {
        var id = PaymentId.CreateUnique();

        Assert.False(string.IsNullOrWhiteSpace(id.Value));
    }

    [Fact]
    public void CreateUnique_CalledTwice_ShouldReturnDifferentValues()
    {
        var id1 = PaymentId.CreateUnique();
        var id2 = PaymentId.CreateUnique();

        Assert.NotEqual(id1.Value, id2.Value);
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnStringValue()
    {
        var id = PaymentId.From("pi_xyz");

        string result = id;

        Assert.Equal("pi_xyz", result);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var id = PaymentId.From("pi_xyz");

        Assert.Equal("pi_xyz", id.ToString());
    }

    [Fact]
    public void Equality_TwoInstancesWithSameValue_ShouldBeEqual()
    {
        var id1 = PaymentId.From("pi_same");
        var id2 = PaymentId.From("pi_same");

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Equality_TwoInstancesWithDifferentValue_ShouldNotBeEqual()
    {
        var id1 = PaymentId.From("pi_aaa");
        var id2 = PaymentId.From("pi_bbb");

        Assert.NotEqual(id1, id2);
    }
}

public class RefundIdTests
{
    [Fact]
    public void From_ShouldCreateInstanceWithProvidedValue()
    {
        var id = RefundId.From("re_abc123");

        Assert.Equal("re_abc123", id.Value);
    }

    [Fact]
    public void From_ShouldTrimWhitespace()
    {
        var id = RefundId.From("  re_abc123  ");

        Assert.Equal("re_abc123", id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void From_WithEmptyValue_ShouldThrowInvalidValueException(string value)
    {
        var ex = Assert.Throws<InvalidValueException>(() => RefundId.From(value));

        Assert.Contains("RefundId cannot be empty", ex.Message);
    }

    [Fact]
    public void CreateUnique_ShouldReturnNonEmptyValue()
    {
        var id = RefundId.CreateUnique();

        Assert.False(string.IsNullOrWhiteSpace(id.Value));
    }

    [Fact]
    public void CreateUnique_CalledTwice_ShouldReturnDifferentValues()
    {
        var id1 = RefundId.CreateUnique();
        var id2 = RefundId.CreateUnique();

        Assert.NotEqual(id1.Value, id2.Value);
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnStringValue()
    {
        var id = RefundId.From("re_xyz");

        string result = id;

        Assert.Equal("re_xyz", result);
    }

    [Fact]
    public void Equality_TwoInstancesWithSameValue_ShouldBeEqual()
    {
        var id1 = RefundId.From("re_same");
        var id2 = RefundId.From("re_same");

        Assert.Equal(id1, id2);
    }
}

public class IdempotencyKeyTests
{
    [Fact]
    public void From_ShouldCreateInstanceWithProvidedValue()
    {
        var key = IdempotencyKey.From("order-123-attempt-1");

        Assert.Equal("order-123-attempt-1", key.Value);
    }

    [Fact]
    public void From_ShouldTrimWhitespace()
    {
        var key = IdempotencyKey.From("  key-abc  ");

        Assert.Equal("key-abc", key.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void From_WithEmptyValue_ShouldThrowInvalidValueException(string value)
    {
        var ex = Assert.Throws<InvalidValueException>(() => IdempotencyKey.From(value));

        Assert.Contains("Idempotency key cannot be empty", ex.Message);
    }

    [Fact]
    public void From_WithValueExceeding128Chars_ShouldThrowInvalidValueException()
    {
        var tooLong = new string('x', 129);

        var ex = Assert.Throws<InvalidValueException>(() => IdempotencyKey.From(tooLong));

        Assert.Contains("128", ex.Message);
    }

    [Fact]
    public void From_WithExactly128Chars_ShouldSucceed()
    {
        var exactly128 = new string('x', 128);

        var key = IdempotencyKey.From(exactly128);

        Assert.Equal(128, key.Value.Length);
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnStringValue()
    {
        var key = IdempotencyKey.From("my-key");

        string result = key;

        Assert.Equal("my-key", result);
    }

    [Fact]
    public void Equality_TwoInstancesWithSameValue_ShouldBeEqual()
    {
        var k1 = IdempotencyKey.From("same-key");
        var k2 = IdempotencyKey.From("same-key");

        Assert.Equal(k1, k2);
    }
}

public class ProviderPaymentIntentIdTests
{
    [Fact]
    public void From_ShouldCreateInstanceWithProvidedValue()
    {
        var id = ProviderPaymentIntentId.From("pi_stripe_abc");

        Assert.Equal("pi_stripe_abc", id.Value);
    }

    [Fact]
    public void From_ShouldTrimWhitespace()
    {
        var id = ProviderPaymentIntentId.From("  pi_stripe_abc  ");

        Assert.Equal("pi_stripe_abc", id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void From_WithEmptyValue_ShouldThrowInvalidValueException(string value)
    {
        var ex = Assert.Throws<InvalidValueException>(() => ProviderPaymentIntentId.From(value));

        Assert.Contains("Provider payment intent id cannot be empty", ex.Message);
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnStringValue()
    {
        var id = ProviderPaymentIntentId.From("pi_abc");

        string result = id;

        Assert.Equal("pi_abc", result);
    }

    [Fact]
    public void Equality_TwoInstancesWithSameValue_ShouldBeEqual()
    {
        var id1 = ProviderPaymentIntentId.From("pi_same");
        var id2 = ProviderPaymentIntentId.From("pi_same");

        Assert.Equal(id1, id2);
    }
}

public class ProviderRefundIdTests
{
    [Fact]
    public void From_ShouldCreateInstanceWithProvidedValue()
    {
        var id = ProviderRefundId.From("re_stripe_abc");

        Assert.Equal("re_stripe_abc", id.Value);
    }

    [Fact]
    public void From_ShouldTrimWhitespace()
    {
        var id = ProviderRefundId.From("  re_stripe_abc  ");

        Assert.Equal("re_stripe_abc", id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void From_WithEmptyValue_ShouldThrowInvalidValueException(string value)
    {
        var ex = Assert.Throws<InvalidValueException>(() => ProviderRefundId.From(value));

        Assert.Contains("Provider refund id cannot be empty", ex.Message);
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnStringValue()
    {
        var id = ProviderRefundId.From("re_abc");

        string result = id;

        Assert.Equal("re_abc", result);
    }

    [Fact]
    public void Equality_TwoInstancesWithSameValue_ShouldBeEqual()
    {
        var id1 = ProviderRefundId.From("re_same");
        var id2 = ProviderRefundId.From("re_same");

        Assert.Equal(id1, id2);
    }
}

using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Tests.Exceptions;

public class DomainExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        var ex = new DomainException("something went wrong");

        Assert.Equal("something went wrong", ex.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
    {
        var inner = new Exception("root cause");

        var ex = new DomainException("outer message", inner);

        Assert.Equal("outer message", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void DomainException_ShouldBeException()
    {
        var ex = new DomainException("msg");

        Assert.IsAssignableFrom<Exception>(ex);
    }
}

public class InvalidValueExceptionTests
{
    [Fact]
    public void Constructor_ShouldSetMessage()
    {
        var ex = new InvalidValueException("value is invalid");

        Assert.Equal("value is invalid", ex.Message);
    }

    [Fact]
    public void InvalidValueException_ShouldBeDomainException()
    {
        var ex = new InvalidValueException("msg");

        Assert.IsAssignableFrom<DomainException>(ex);
    }
}

public class InvalidPaymentStateTransitionExceptionTests
{
    [Fact]
    public void Constructor_MessageShouldContainFromAndToStatus()
    {
        var ex = new InvalidPaymentStateTransitionException(
            PaymentStatus.Created, PaymentStatus.Refunded);

        Assert.Contains("Created", ex.Message);
        Assert.Contains("Refunded", ex.Message);
    }

    [Fact]
    public void InvalidPaymentStateTransitionException_ShouldBeDomainException()
    {
        var ex = new InvalidPaymentStateTransitionException(
            PaymentStatus.Created, PaymentStatus.Refunded);

        Assert.IsAssignableFrom<DomainException>(ex);
    }
}

public class InvalidRefundStateTransitionExceptionTests
{
    [Fact]
    public void Constructor_MessageShouldContainFromAndToStatus()
    {
        var ex = new InvalidRefundStateTransitionException(
            RefundStatus.Requested, RefundStatus.Succeeded);

        Assert.Contains("Requested", ex.Message);
        Assert.Contains("Succeeded", ex.Message);
    }

    [Fact]
    public void InvalidRefundStateTransitionException_ShouldBeDomainException()
    {
        var ex = new InvalidRefundStateTransitionException(
            RefundStatus.Requested, RefundStatus.Succeeded);

        Assert.IsAssignableFrom<DomainException>(ex);
    }
}

public class UniqueConstraintViolationExceptionTests
{
    [Fact]
    public void Constructor_WithConstraintName_ShouldSetConstraintNameProperty()
    {
        var inner = new Exception("db error");

        var ex = new UniqueConstraintViolationException("IX_payments_idempotency_key", inner);

        Assert.Equal("IX_payments_idempotency_key", ex.ConstraintName);
    }

    [Fact]
    public void Constructor_WithConstraintName_MessageShouldContainConstraintName()
    {
        var inner = new Exception("db error");

        var ex = new UniqueConstraintViolationException("IX_payments_idempotency_key", inner);

        Assert.Contains("IX_payments_idempotency_key", ex.Message);
    }

    [Fact]
    public void Constructor_WithNullConstraintName_ShouldSetConstraintNameToNull()
    {
        var inner = new Exception("db error");

        var ex = new UniqueConstraintViolationException(null, inner);

        Assert.Null(ex.ConstraintName);
    }

    [Fact]
    public void Constructor_WithNullConstraintName_MessageShouldBeGeneric()
    {
        var inner = new Exception("db error");

        var ex = new UniqueConstraintViolationException(null, inner);

        Assert.Contains("Unique constraint violation", ex.Message);
    }

    [Fact]
    public void Constructor_ShouldPreserveInnerException()
    {
        var inner = new Exception("original db error");

        var ex = new UniqueConstraintViolationException("some_constraint", inner);

        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void UniqueConstraintViolationException_ShouldBeDomainException()
    {
        var ex = new UniqueConstraintViolationException(null, new Exception("e"));

        Assert.IsAssignableFrom<DomainException>(ex);
    }
}

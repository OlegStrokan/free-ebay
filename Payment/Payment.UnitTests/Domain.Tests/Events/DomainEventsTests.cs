using Domain.Common;
using Domain.Enums;
using Domain.Events;
using Domain.ValueObjects;

namespace Domain.Tests.Events;

public class PaymentCreatedEventTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var paymentId = PaymentId.CreateUnique();
        var amount = Money.Create(100, "USD");
        var createdAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var evt = new PaymentCreatedEvent(paymentId, "order-1", "cust-1", amount, PaymentMethod.Card, createdAt);

        Assert.Equal(paymentId, evt.PaymentId);
        Assert.Equal("order-1", evt.OrderId);
        Assert.Equal("cust-1", evt.CustomerId);
        Assert.Equal(amount, evt.Amount);
        Assert.Equal(PaymentMethod.Card, evt.PaymentMethod);
        Assert.Equal(createdAt, evt.CreatedAt);
    }

    [Fact]
    public void OccurredOn_ShouldEqualCreatedAt()
    {
        var createdAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var evt = new PaymentCreatedEvent(PaymentId.CreateUnique(), "o", "c", Money.Create(10, "USD"), PaymentMethod.Card, createdAt);

        Assert.Equal(createdAt, evt.OccurredOn);
    }

    [Fact]
    public void EventId_ShouldBeNonEmpty()
    {
        var evt = new PaymentCreatedEvent(PaymentId.CreateUnique(), "o", "c", Money.Create(10, "USD"), PaymentMethod.Card, DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, evt.EventId);
    }

    [Fact]
    public void TwoInstances_ShouldHaveDifferentEventIds()
    {
        var evt1 = new PaymentCreatedEvent(PaymentId.CreateUnique(), "o", "c", Money.Create(10, "USD"), PaymentMethod.Card, DateTime.UtcNow);
        var evt2 = new PaymentCreatedEvent(PaymentId.CreateUnique(), "o", "c", Money.Create(10, "USD"), PaymentMethod.Card, DateTime.UtcNow);

        Assert.NotEqual(evt1.EventId, evt2.EventId);
    }

    [Fact]
    public void PaymentCreatedEvent_ShouldImplementIDomainEvent()
    {
        var evt = new PaymentCreatedEvent(PaymentId.CreateUnique(), "o", "c", Money.Create(10, "USD"), PaymentMethod.Card, DateTime.UtcNow);

        Assert.IsAssignableFrom<IDomainEvent>(evt);
    }
}

public class PaymentFailedEventTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var paymentId = PaymentId.CreateUnique();
        var reason = FailureReason.Create("card_declined", "Card declined");
        var failedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var evt = new PaymentFailedEvent(paymentId, reason, failedAt);

        Assert.Equal(paymentId, evt.PaymentId);
        Assert.Equal(reason, evt.FailureReason);
        Assert.Equal(failedAt, evt.FailedAt);
    }

    [Fact]
    public void OccurredOn_ShouldEqualFailedAt()
    {
        var failedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var evt = new PaymentFailedEvent(PaymentId.CreateUnique(), FailureReason.Create(null, "fail"), failedAt);

        Assert.Equal(failedAt, evt.OccurredOn);
    }

    [Fact]
    public void EventId_ShouldBeNonEmpty()
    {
        var evt = new PaymentFailedEvent(PaymentId.CreateUnique(), FailureReason.Create(null, "fail"), DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, evt.EventId);
    }
}

public class PaymentPendingProviderConfirmationEventTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var paymentId = PaymentId.CreateUnique();
        var intentId = ProviderPaymentIntentId.From("pi_stripe_abc");
        var pendingAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var evt = new PaymentPendingProviderConfirmationEvent(paymentId, intentId, pendingAt);

        Assert.Equal(paymentId, evt.PaymentId);
        Assert.Equal(intentId, evt.ProviderPaymentIntentId);
        Assert.Equal(pendingAt, evt.PendingAt);
    }

    [Fact]
    public void OccurredOn_ShouldEqualPendingAt()
    {
        var pendingAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var evt = new PaymentPendingProviderConfirmationEvent(
            PaymentId.CreateUnique(), ProviderPaymentIntentId.From("pi_abc"), pendingAt);

        Assert.Equal(pendingAt, evt.OccurredOn);
    }

    [Fact]
    public void EventId_ShouldBeNonEmpty()
    {
        var evt = new PaymentPendingProviderConfirmationEvent(
            PaymentId.CreateUnique(), ProviderPaymentIntentId.From("pi_abc"), DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, evt.EventId);
    }
}

public class PaymentSucceededEventTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var paymentId = PaymentId.CreateUnique();
        var intentId = ProviderPaymentIntentId.From("pi_stripe_abc");
        var succeededAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var evt = new PaymentSucceededEvent(paymentId, intentId, succeededAt);

        Assert.Equal(paymentId, evt.PaymentId);
        Assert.Equal(intentId, evt.ProviderPaymentIntentId);
        Assert.Equal(succeededAt, evt.SucceededAt);
    }

    [Fact]
    public void Constructor_WithNullProviderPaymentIntentId_ShouldSucceed()
    {
        var evt = new PaymentSucceededEvent(PaymentId.CreateUnique(), null, DateTime.UtcNow);

        Assert.Null(evt.ProviderPaymentIntentId);
    }

    [Fact]
    public void OccurredOn_ShouldEqualSucceededAt()
    {
        var succeededAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var evt = new PaymentSucceededEvent(PaymentId.CreateUnique(), null, succeededAt);

        Assert.Equal(succeededAt, evt.OccurredOn);
    }

    [Fact]
    public void EventId_ShouldBeNonEmpty()
    {
        var evt = new PaymentSucceededEvent(PaymentId.CreateUnique(), null, DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, evt.EventId);
    }
}

public class RefundRequestedEventTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var paymentId = PaymentId.CreateUnique();
        var refundId = RefundId.CreateUnique();
        var amount = Money.Create(50, "USD");
        var requestedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var evt = new RefundRequestedEvent(paymentId, refundId, amount, "Customer request", requestedAt);

        Assert.Equal(paymentId, evt.PaymentId);
        Assert.Equal(refundId, evt.RefundId);
        Assert.Equal(amount, evt.Amount);
        Assert.Equal("Customer request", evt.Reason);
        Assert.Equal(requestedAt, evt.RequestedAt);
    }

    [Fact]
    public void OccurredOn_ShouldEqualRequestedAt()
    {
        var requestedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var evt = new RefundRequestedEvent(PaymentId.CreateUnique(), RefundId.CreateUnique(), Money.Create(10, "USD"), "reason", requestedAt);

        Assert.Equal(requestedAt, evt.OccurredOn);
    }

    [Fact]
    public void EventId_ShouldBeNonEmpty()
    {
        var evt = new RefundRequestedEvent(PaymentId.CreateUnique(), RefundId.CreateUnique(), Money.Create(10, "USD"), "reason", DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, evt.EventId);
    }
}

public class RefundSucceededEventTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var paymentId = PaymentId.CreateUnique();
        var refundId = RefundId.CreateUnique();
        var providerRefundId = ProviderRefundId.From("re_stripe_abc");
        var succeededAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var evt = new RefundSucceededEvent(paymentId, refundId, providerRefundId, succeededAt);

        Assert.Equal(paymentId, evt.PaymentId);
        Assert.Equal(refundId, evt.RefundId);
        Assert.Equal(providerRefundId, evt.ProviderRefundId);
        Assert.Equal(succeededAt, evt.SucceededAt);
    }

    [Fact]
    public void Constructor_WithNullProviderRefundId_ShouldSucceed()
    {
        var evt = new RefundSucceededEvent(PaymentId.CreateUnique(), RefundId.CreateUnique(), null, DateTime.UtcNow);

        Assert.Null(evt.ProviderRefundId);
    }

    [Fact]
    public void OccurredOn_ShouldEqualSucceededAt()
    {
        var succeededAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var evt = new RefundSucceededEvent(PaymentId.CreateUnique(), RefundId.CreateUnique(), null, succeededAt);

        Assert.Equal(succeededAt, evt.OccurredOn);
    }

    [Fact]
    public void EventId_ShouldBeNonEmpty()
    {
        var evt = new RefundSucceededEvent(PaymentId.CreateUnique(), RefundId.CreateUnique(), null, DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, evt.EventId);
    }
}

public class RefundFailedEventTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var paymentId = PaymentId.CreateUnique();
        var refundId = RefundId.CreateUnique();
        var reason = FailureReason.Create("err", "refund failed");
        var failedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var evt = new RefundFailedEvent(paymentId, refundId, reason, failedAt);

        Assert.Equal(paymentId, evt.PaymentId);
        Assert.Equal(refundId, evt.RefundId);
        Assert.Equal(reason, evt.FailureReason);
        Assert.Equal(failedAt, evt.FailedAt);
    }

    [Fact]
    public void OccurredOn_ShouldEqualFailedAt()
    {
        var failedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var evt = new RefundFailedEvent(PaymentId.CreateUnique(), RefundId.CreateUnique(), FailureReason.Create(null, "fail"), failedAt);

        Assert.Equal(failedAt, evt.OccurredOn);
    }

    [Fact]
    public void EventId_ShouldBeNonEmpty()
    {
        var evt = new RefundFailedEvent(PaymentId.CreateUnique(), RefundId.CreateUnique(), FailureReason.Create(null, "fail"), DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, evt.EventId);
    }
}

public class OrderCallbackQueuedEventTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var paymentId = PaymentId.CreateUnique();
        var queuedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var evt = new OrderCallbackQueuedEvent(paymentId, "evt-001", "payment.succeeded", "order-123", queuedAt);

        Assert.Equal(paymentId, evt.PaymentId);
        Assert.Equal("evt-001", evt.CallbackEventId);
        Assert.Equal("payment.succeeded", evt.CallbackType);
        Assert.Equal("order-123", evt.OrderId);
        Assert.Equal(queuedAt, evt.QueuedAt);
    }

    [Fact]
    public void OccurredOn_ShouldEqualQueuedAt()
    {
        var queuedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var evt = new OrderCallbackQueuedEvent(PaymentId.CreateUnique(), "evt-001", "payment.succeeded", "order-1", queuedAt);

        Assert.Equal(queuedAt, evt.OccurredOn);
    }

    [Fact]
    public void EventId_ShouldBeNonEmpty()
    {
        var evt = new OrderCallbackQueuedEvent(PaymentId.CreateUnique(), "evt-001", "type", "order-1", DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, evt.EventId);
    }

    [Fact]
    public void TwoInstances_ShouldHaveDifferentEventIds()
    {
        var evt1 = new OrderCallbackQueuedEvent(PaymentId.CreateUnique(), "e1", "t", "o", DateTime.UtcNow);
        var evt2 = new OrderCallbackQueuedEvent(PaymentId.CreateUnique(), "e2", "t", "o", DateTime.UtcNow);

        Assert.NotEqual(evt1.EventId, evt2.EventId);
    }

    [Fact]
    public void OrderCallbackQueuedEvent_ShouldImplementIDomainEvent()
    {
        var evt = new OrderCallbackQueuedEvent(PaymentId.CreateUnique(), "e", "t", "o", DateTime.UtcNow);

        Assert.IsAssignableFrom<IDomainEvent>(evt);
    }
}

using Domain.Entities.Subscription;
using Domain.Events.Subscription;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Tests.Entities;

public class RecurringOrderTests
{
    private readonly CustomerId _customerId = CustomerId.CreateUnique();
    private readonly Address _address = Address.Create("Náměstí Míru 1", "Prague", "CZ", "12000");
    private readonly ProductId _productId = ProductId.CreateUnique();
    private readonly Money _price = Money.Create(50m, "USD");
    private const string PaymentMethod = "Card-123";

    private List<RecurringOrderItem> DefaultItems() =>
        new() { RecurringOrderItem.Create(_productId, 3, _price) };

    private RecurringOrder CreateActive(
        ScheduleFrequency? frequency = null,
        DateTime? firstRunAt = null,
        int? maxExecutions = null) =>
        RecurringOrder.Create(
            _customerId,
            frequency ?? ScheduleFrequency.Weekly,
            DefaultItems(),
            _address,
            PaymentMethod,
            firstRunAt,
            maxExecutions);
    
    [Fact]
    public void Create_ShouldRaiseCreatedEvent_AndSetStatusToActive()
    {
        var order = CreateActive();

        var evt = Assert.Single(order.UncommitedEvents) as RecurringOrderCreatedEvent;
        Assert.NotNull(evt);
        Assert.Equal(_customerId, evt.CustomerId);
        Assert.Equal(PaymentMethod, evt.PaymentMethod);
        Assert.Equal("Weekly", evt.Frequency);

        Assert.Equal(RecurringOrderStatus.Active, order.Status);
        Assert.Equal(1, order.Version);
    }

    [Fact]
    public void Create_ShouldStoreItems_AndIncrementVersion()
    {
        var order = CreateActive();

        Assert.Single(order.Items);
        Assert.Equal(_productId, order.Items[0].ProductId);
        Assert.Equal(3, order.Items[0].Quantity);
    }

    [Fact]
    public void Create_ShouldThrow_WhenItemsListIsEmpty()
    {
        Assert.Throws<DomainException>(() =>
            RecurringOrder.Create(_customerId, ScheduleFrequency.Weekly, new List<RecurringOrderItem>(),
                _address, PaymentMethod));
    }

    [Fact]
    public void Create_ShouldThrow_WhenPaymentMethodIsEmpty()
    {
        Assert.Throws<DomainException>(() =>
            RecurringOrder.Create(_customerId, ScheduleFrequency.Weekly, DefaultItems(), _address, ""));
    }

    [Fact]
    public void Create_ShouldThrow_WhenPaymentMethodIsWhitespace()
    {
        Assert.Throws<DomainException>(() =>
            RecurringOrder.Create(_customerId, ScheduleFrequency.Weekly, DefaultItems(), _address, "   "));
    }

    [Fact]
    public void Create_ShouldThrow_WhenMaxExecutionsIsZero()
    {
        Assert.Throws<DomainException>(() =>
            RecurringOrder.Create(_customerId, ScheduleFrequency.Weekly, DefaultItems(), _address,
                PaymentMethod, maxExecutions: 0));
    }

    [Fact]
    public void Create_ShouldThrow_WhenMaxExecutionsIsNegative()
    {
        Assert.Throws<DomainException>(() =>
            RecurringOrder.Create(_customerId, ScheduleFrequency.Weekly, DefaultItems(), _address,
                PaymentMethod, maxExecutions: -5));
    }

    [Fact]
    public void Create_ShouldUseProvidedFirstRunAt_WhenSpecified()
    {
        var firstRun = DateTime.UtcNow.AddDays(5);
        var order = CreateActive(firstRunAt: firstRun);

        Assert.Equal(firstRun, order.NextRunAt, precision: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_ShouldScheduleNextRunAt_UsingFrequency_WhenFirstRunAtIsNull()
    {
        var before = DateTime.UtcNow;
        var order = CreateActive(frequency: ScheduleFrequency.Weekly); // 7 days

        Assert.True(order.NextRunAt >= before.AddDays(7));
        Assert.True(order.NextRunAt <= DateTime.UtcNow.AddDays(7).AddSeconds(5));
    }
    
    [Fact]
    public void Pause_ShouldTransitionToPaused_WhenActive()
    {
        var order = CreateActive();
        order.ClearUncommittedEvents();

        order.Pause();

        Assert.Equal(RecurringOrderStatus.Paused, order.Status);
        Assert.IsType<RecurringOrderPausedEvent>(order.UncommitedEvents.Last());
        Assert.Equal(2, order.Version);
    }

    [Fact]
    public void Pause_ShouldThrow_WhenAlreadyPaused()
    {
        var order = CreateActive();
        order.Pause();

        var ex = Assert.Throws<DomainException>(() => order.Pause());
        Assert.Contains("Cannot transition RecurringOrder from 'Paused' to 'Paused'", ex.Message);
    }

    [Fact]
    public void Pause_ShouldThrow_WhenCancelled()
    {
        var order = CreateActive();
        order.Cancel("test reason");

        var ex = Assert.Throws<DomainException>(() => order.Pause());
        Assert.Contains("Cannot transition", ex.Message);
    }
    
    [Fact]
    public void Resume_ShouldTransitionToActive_WhenPaused()
    {
        var order = CreateActive();
        order.Pause();
        order.ClearUncommittedEvents();

        order.Resume();

        Assert.Equal(RecurringOrderStatus.Active, order.Status);
        Assert.IsType<RecurringOrderResumedEvent>(order.UncommitedEvents.Last());
    }

    [Fact]
    public void Resume_ShouldRecalculateNextRunAt()
    {
        var before = DateTime.UtcNow;
        var order = CreateActive(frequency: ScheduleFrequency.Monthly);
        order.Pause();
        order.Resume();

        // NextRunAt must be ~30 days from now
        Assert.True(order.NextRunAt >= before.AddDays(29));
    }

    [Fact]
    public void Resume_ShouldThrow_WhenAlreadyActive()
    {
        var order = CreateActive();

        var ex = Assert.Throws<DomainException>(() => order.Resume());
        Assert.Contains("Cannot transition", ex.Message);
    }

    [Fact]
    public void Resume_ShouldThrow_WhenCancelled()
    {
        var order = CreateActive();
        order.Cancel("reason");

        var ex = Assert.Throws<DomainException>(() => order.Resume());
        Assert.Contains("Cannot transition", ex.Message);
    }
    
    [Fact]
    public void Cancel_ShouldTransitionToCancelled_WhenActive()
    {
        var order = CreateActive();
        order.ClearUncommittedEvents();

        order.Cancel("Out of budget");

        Assert.Equal(RecurringOrderStatus.Cancelled, order.Status);
        var evt = Assert.IsType<RecurringOrderCancelledEvent>(order.UncommitedEvents.Last());
        Assert.Equal("Out of budget", evt.Reason);
    }

    [Fact]
    public void Cancel_ShouldTransitionToCancelled_WhenPaused()
    {
        var order = CreateActive();
        order.Pause();
        order.ClearUncommittedEvents();

        order.Cancel("No longer needed");

        Assert.Equal(RecurringOrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenAlreadyCancelled()
    {
        var order = CreateActive();
        order.Cancel("first");

        var ex = Assert.Throws<DomainException>(() => order.Cancel("second"));
        Assert.Contains("Cannot transition", ex.Message);
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenReasonIsEmpty()
    {
        var order = CreateActive();

        Assert.Throws<DomainException>(() => order.Cancel(""));
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenReasonIsWhitespace()
    {
        var order = CreateActive();

        Assert.Throws<DomainException>(() => order.Cancel("   "));
    }
    
    [Fact]
    public void RecordExecution_ShouldIncrementTotalExecutions_AndSetLastRunAt()
    {
        var order = CreateActive();
        order.ClearUncommittedEvents();
        var orderId = Guid.NewGuid();

        order.RecordExecution(orderId);

        Assert.Equal(1, order.TotalExecutions);
        Assert.NotNull(order.LastRunAt);
        var evt = Assert.IsType<RecurringOrderExecutedEvent>(order.UncommitedEvents.Last());
        Assert.Equal(1, evt.ExecutionNumber);
        Assert.Equal(orderId, evt.CreatedOrderId);
    }

    [Fact]
    public void RecordExecution_ShouldCalculateNextRunAt_BasedOnFrequency()
    {
        var order = CreateActive(frequency: ScheduleFrequency.Weekly);
        var before = DateTime.UtcNow;

        order.RecordExecution(Guid.NewGuid());

        Assert.True(order.NextRunAt >= before.AddDays(7));
    }

    [Fact]
    public void RecordExecution_ShouldAutoCancelWhenMaxExecutionsReached()
    {
        var order = CreateActive(maxExecutions: 1);

        order.RecordExecution(Guid.NewGuid());

        Assert.Equal(RecurringOrderStatus.Cancelled, order.Status);
        Assert.Equal(1, order.TotalExecutions);
    }

    [Fact]
    public void RecordExecution_ShouldNotCancel_WhenBelowMaxExecutions()
    {
        var order = CreateActive(maxExecutions: 3);

        order.RecordExecution(Guid.NewGuid());

        Assert.Equal(RecurringOrderStatus.Active, order.Status);
        Assert.Equal(1, order.TotalExecutions);
    }

    [Fact]
    public void RecordExecution_ShouldThrow_WhenNotActive()
    {
        var order = CreateActive();
        order.Pause();

        Assert.Throws<DomainException>(() => order.RecordExecution(Guid.NewGuid()));
    }
    
    [Fact]
    public void RecordExecutionFailure_ShouldRescheduleNextRunAtOneHourLater()
    {
        var order = CreateActive();
        var before = DateTime.UtcNow;
        order.ClearUncommittedEvents();

        order.RecordExecutionFailure("Payment gateway timeout");

        var evt = Assert.IsType<RecurringOrderExecutionFailedEvent>(order.UncommitedEvents.Last());
        Assert.Equal("Payment gateway timeout", evt.Reason);
        Assert.True(evt.NextRetryAt >= before.AddHours(1));
    }

    [Fact]
    public void RecordExecutionFailure_ShouldNotCancelOrPause()
    {
        var order = CreateActive();

        order.RecordExecutionFailure("gateway error");

        Assert.Equal(RecurringOrderStatus.Active, order.Status);
    }

    [Fact]
    public void RecordExecutionFailure_ShouldThrow_WhenNotActive()
    {
        var order = CreateActive();
        order.Pause();

        Assert.Throws<DomainException>(() => order.RecordExecutionFailure("reason"));
    }
    
    [Fact]
    public void IsDue_ShouldReturnTrue_WhenActiveAndNextRunAtIsInPast()
    {
        var order = CreateActive(firstRunAt: DateTime.UtcNow.AddMinutes(-1));

        Assert.True(order.IsDue);
    }

    [Fact]
    public void IsDue_ShouldReturnFalse_WhenActiveButNextRunAtIsInFuture()
    {
        var order = CreateActive(firstRunAt: DateTime.UtcNow.AddDays(1));

        Assert.False(order.IsDue);
    }

    [Fact]
    public void IsDue_ShouldReturnFalse_WhenPaused_EvenIfNextRunAtIsInPast()
    {
        var order = CreateActive(firstRunAt: DateTime.UtcNow.AddMinutes(-1));
        order.Pause();

        Assert.False(order.IsDue);
    }

    [Fact]
    public void IsDue_ShouldReturnFalse_WhenCancelled()
    {
        var order = CreateActive(firstRunAt: DateTime.UtcNow.AddMinutes(-1));
        order.Cancel("reason");

        Assert.False(order.IsDue);
    }

    // snapshotik

    [Fact]
    public void ToSnapshotState_AndFromSnapshot_ShouldProduceEquivalentAggregate()
    {
        var order = CreateActive(
            frequency: ScheduleFrequency.Monthly,
            firstRunAt: DateTime.UtcNow.AddDays(3),
            maxExecutions: 12);
        order.RecordExecution(Guid.NewGuid());

        var snapshot = order.ToSnapshotState();
        var restored = RecurringOrder.FromSnapshot(snapshot);

        Assert.Equal(order.Id.Value, restored.Id.Value);
        Assert.Equal(order.CustomerId.Value, restored.CustomerId.Value);
        Assert.Equal(order.PaymentMethod, restored.PaymentMethod);
        Assert.Equal(order.Status.Name, restored.Status.Name);
        Assert.Equal(order.TotalExecutions, restored.TotalExecutions);
        Assert.Equal(order.MaxExecutions, restored.MaxExecutions);
        Assert.Equal(order.Frequency.Name, restored.Frequency.Name);
        Assert.Equal(order.Items.Count, restored.Items.Count);
        Assert.Equal(order.Version, restored.Version);
    }

    [Fact]
    public void FromHistory_ShouldReconstructAllStateFromEvents()
    {
        var order = CreateActive();
        order.Pause();
        order.Resume();
        order.RecordExecution(Guid.NewGuid());

        var history = order.UncommitedEvents.ToList();
        var restored = RecurringOrder.FromHistory(history);

        Assert.Equal(order.Id.Value, restored.Id.Value);
        Assert.Equal(order.Status.Name, restored.Status.Name);
        Assert.Equal(order.TotalExecutions, restored.TotalExecutions);
        Assert.Equal(order.Version, restored.Version);
    }

    [Fact]
    public void FromHistory_ShouldResultInCancelledStatus_AfterCancelEvent()
    {
        var order = CreateActive();
        order.Cancel("budget cut");

        var restored = RecurringOrder.FromHistory(order.UncommitedEvents);

        Assert.Equal(RecurringOrderStatus.Cancelled, restored.Status);
    }
}

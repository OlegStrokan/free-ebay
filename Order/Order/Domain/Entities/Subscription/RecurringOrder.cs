using Domain.Common;
using Domain.Events.Subscription;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities.Subscription;

public sealed class RecurringOrder : AggregateRoot<RecurringOrderId>
{
    private CustomerId _customerId = null!;
    private string _paymentMethod = string.Empty;
    private Address _deliveryAddress = null!;
    private ScheduleFrequency _frequency = null!;
    private List<RecurringOrderItem> _items = new();
    private RecurringOrderStatus _status  = null!;
    private DateTime _nextRunAt;
    private DateTime? _lastRunAt;
    private int _totalExecutions;
    private int? _maxExecutions;
    private DateTime _createdAt;
    private DateTime? _updatedAt;


    public CustomerId CustomerId => _customerId;
    public string PaymentMethod => _paymentMethod;
    public Address DeliveryAddress => _deliveryAddress;
    public ScheduleFrequency Frequency => _frequency;
    public RecurringOrderStatus Status => _status;
    public DateTime NextRunAt => _nextRunAt;
    public DateTime? LastRunAt => _lastRunAt;
    public int TotalExecutions => _totalExecutions;
    public int? MaxExecutions => _maxExecutions;

    public IReadOnlyList<RecurringOrderItem> Items => _items.AsReadOnly();

    public bool IsDue =>
        _status == RecurringOrderStatus.Active && DateTime.UtcNow >= _nextRunAt;

    private RecurringOrder() { }


    private RecurringOrder(RecurringOrderSnapshotState s)
    {
        Id = RecurringOrderId.From(s.Id);
        _customerId = CustomerId.From(s.CustomerId);
        _paymentMethod = s.PaymentMethod;
        _deliveryAddress = Address.Create(s.Street, s.City, s.Country, s.PostalCode);
        _frequency = ScheduleFrequency.FromName(s.Frequency);
        _status = RecurringOrderStatus.FromName(s.Status);
        _nextRunAt = s.NextRunAt;
        _lastRunAt = s.LastRunAt;
        _totalExecutions = s.TotalExecutions;
        _maxExecutions = s.MaxExecutions;
        _createdAt = s.CreatedAt;
        _updatedAt = s.UpdatedAt;
        _items = s.Items
            .Select(i => RecurringOrderItem.Create(
                ProductId.From(i.ProductId),
                i.Quantity,
                Money.Create(i.Price, i.Currency)))
            .ToList();
        RestoreVersion(s.Version);
    }

    public RecurringOrderSnapshotState ToSnapshotState() => new(
        Id: Id.Value,
        CustomerId: _customerId.Value,
        PaymentMethod: _paymentMethod,
        Frequency: _frequency.Name,
        Status: _status.Name,
        NextRunAt: _nextRunAt,
        LastRunAt: _lastRunAt,
        TotalExecutions:_totalExecutions,
        MaxExecutions: _maxExecutions,
        Street: _deliveryAddress.Street,
        City: _deliveryAddress.City,
        Country: _deliveryAddress.Country,
        PostalCode: _deliveryAddress.PostalCode,
        CreatedAt: _createdAt,
        UpdatedAt: _updatedAt,
        Version: Version,
        Items: _items.Select(i => new RecurringOrderItemSnapshotState(
            ProductId: i.ProductId.Value,
            Quantity:  i.Quantity,
            Price:     i.Price.Amount,
            Currency:  i.Price.Currency)).ToList());

    public static RecurringOrder FromSnapshot(RecurringOrderSnapshotState state) => new(state);

    public static RecurringOrder FromHistory(IEnumerable<IDomainEvent> history)
    {
        var order = new RecurringOrder();
        order.LoadFromHistory(history);
        return order;
    }
    
    public static RecurringOrder Create(
        CustomerId customerId,
        ScheduleFrequency frequency,
        List<RecurringOrderItem> items,
        Address deliveryAddress,
        string paymentMethod,
        DateTime? firstRunAt  = null,
        int?      maxExecutions = null)
    {
        if (items == null || !items.Any())
            throw new DomainException("Recurring order must have at least one item");

        if (string.IsNullOrWhiteSpace(paymentMethod))
            throw new DomainException("Payment method is required");

        if (maxExecutions.HasValue && maxExecutions.Value <= 0)
            throw new DomainException("MaxExecutions must be positive if specified");

        var nextRunAt = firstRunAt ?? ScheduleFrequency.CalculateNextRunAt(DateTime.UtcNow, frequency.Name);
        var order = new RecurringOrder();

        order.RaiseEvent(new RecurringOrderCreatedEvent(
            RecurringOrderId.CreateUnique(),
            customerId,
            frequency.Name,
            items,
            deliveryAddress,
            paymentMethod,
            nextRunAt,
            maxExecutions,
            DateTime.UtcNow));

        return order;
    }
    
    public void Pause()
    {
        _status.ValidateTransitionTo(RecurringOrderStatus.Paused);
        RaiseEvent(new RecurringOrderPausedEvent(Id, DateTime.UtcNow));
    }

    public void Resume()
    {
        _status.ValidateTransitionTo(RecurringOrderStatus.Active);
        var nextRunAt = ScheduleFrequency.CalculateNextRunAt(DateTime.UtcNow, _frequency.Name);
        RaiseEvent(new RecurringOrderResumedEvent(Id, nextRunAt, DateTime.UtcNow));
    }

    public void Cancel(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Cancellation reason is required");

        _status.ValidateTransitionTo(RecurringOrderStatus.Cancelled);
        RaiseEvent(new RecurringOrderCancelledEvent(Id, reason, DateTime.UtcNow));
    }
    
    public void RecordExecution(Guid createdOrderId)
    {
        if (_status != RecurringOrderStatus.Active)
            throw new DomainException("Cannot record execution on a non-active recurring order");

        var executionNumber = _totalExecutions + 1;
        var nextRunAt = ScheduleFrequency.CalculateNextRunAt(DateTime.UtcNow, _frequency.Name);

        RaiseEvent(new RecurringOrderExecutedEvent(
            Id, createdOrderId, executionNumber, nextRunAt, DateTime.UtcNow));
    }
    
    public void RecordExecutionFailure(string reason)
    {
        if (_status != RecurringOrderStatus.Active)
            throw new DomainException("Cannot record failure on a non-active recurring order");

        var nextRetryAt = DateTime.UtcNow.AddHours(1);
        RaiseEvent(new RecurringOrderExecutionFailedEvent(
            Id, reason, nextRetryAt, DateTime.UtcNow));
    }


    private void Apply(RecurringOrderCreatedEvent evt)
    {
        Id = evt.RecurringOrderId;
        _customerId = evt.CustomerId;
        _paymentMethod = evt.PaymentMethod;
        _deliveryAddress = evt.DeliveryAddress;
        _frequency = ScheduleFrequency.FromName(evt.Frequency);
        _status = RecurringOrderStatus.Active;
        _nextRunAt = evt.NextRunAt;
        _maxExecutions = evt.MaxExecutions;
        _totalExecutions = 0;
        _items = evt.Items.ToList();
        _createdAt = evt.CreatedAt;
    }

    private void Apply(RecurringOrderPausedEvent evt)
    {
        _status = RecurringOrderStatus.Paused;
        _updatedAt = evt.PausedAt;
    }

    private void Apply(RecurringOrderResumedEvent evt)
    {
        _status = RecurringOrderStatus.Active;
        _nextRunAt = evt.NextRunAt;
        _updatedAt = evt.ResumedAt;
    }

    private void Apply(RecurringOrderCancelledEvent evt)
    {
        _status = RecurringOrderStatus.Cancelled;
        _updatedAt = evt.CancelledAt;
    }

    private void Apply(RecurringOrderExecutedEvent evt)
    {
        _totalExecutions = evt.ExecutionNumber;
        _lastRunAt = evt.ExecutedAt;
        _nextRunAt = evt.NextRunAt;
        _updatedAt = evt.ExecutedAt;

        // auto-cancel once max executions reached
        if (_maxExecutions.HasValue && _totalExecutions >= _maxExecutions.Value)
            _status = RecurringOrderStatus.Cancelled;
    }

    private void Apply(RecurringOrderExecutionFailedEvent evt)
    {
        _nextRunAt = evt.NextRetryAt;
        _updatedAt = evt.FailedAt;
    }
}

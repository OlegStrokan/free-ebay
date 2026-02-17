# Order Microservice - Architectural Review

**Review Date:** January 31, 2026  
**Scope:** src/Application, src/Domain, src/Infrastructure  
**Focus Areas:** Domain Entity (Order), Saga Architecture, Separation of Concerns, Industry Comparison

---

## Executive Summary

This Order microservice demonstrates **solid fundamentals** in Domain-Driven Design, Event Sourcing, and Saga Pattern implementation. The transactional outbox pattern, resumable sagas, and proper value object usage show mature architectural thinking. However, there are significant areas for improvement around **aggregate design, code duplication, and operational concerns** that would need addressing before this could scale to FAANG-level production loads.

**Overall Assessment: 7/10** - Good foundation, needs refinement for production scale.

---

## Table of Contents

1. [Good Parts](#good-parts-)
2. [Domain Layer Analysis](#domain-layer-analysis)
3. [Application Layer Analysis](#application-layer-analysis)
4. [Infrastructure Layer Analysis](#infrastructure-layer-analysis)
5. [Code Smells & Antipatterns](#code-smells--antipatterns)
6. [FAANG Comparison](#faang-comparison-how-big-tech-handles-this)
7. [Detailed Recommendations](#detailed-recommendations)
8. [Priority Action Items](#priority-action-items)

---

## Good Parts ✅

### 1. Solid Event Sourcing Implementation

The Order entity correctly implements event sourcing patterns:

```csharp
// ✅ Proper event application pattern
private void RaiseEvent(IDomainEvent evt)
{
    ApplyEvent(evt);
    _uncommitedEvents.Add(evt);
}

// ✅ Aggregate reconstruction from events
public static Order FromEvents(IEnumerable<IDomainEvent> history)
{
    var order = new Order();
    foreach (var evt in history) order.ApplyEvent(evt);
    return order;
}
```

**Why this is good:** Events are the source of truth, state is derived, and the aggregate can be rebuilt at any point.

### 2. Value Objects Done Right

```csharp
// ✅ Immutable record with validation
public sealed record Money
{
    public Money(decimal amount, string currency)
    {
        if (amount < 0) 
            throw new ArgumentException("Money amount cannot be negative");
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }
}
```

**Why this is good:** 
- Immutability prevents accidental state corruption
- Validation in constructor enforces invariants
- Business logic methods (`Add`, `Subtract`) are pure and return new instances

### 3. Transactional Outbox Pattern

```csharp
// ✅ Atomic transaction: aggregate + outbox
await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
await orderRepository.AddAsync(order, cancellationToken);
foreach (var domainEvent in order.UncommitedEvents)
{
    await outboxRepository.AddAsync(/*...*/);
}
await unitOfWork.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

**Why this is good:** Guarantees exactly-once event publishing without distributed transactions.

### 4. Resumable Saga Architecture

```csharp
// ✅ Saga can pause and resume for external webhooks
if (stepResult.Metadata.TryGetValue("SagaState", out var sagaStateValue) &&
    sagaStateValue.ToString() == "WaitingForEvent")
{
    sagaState.Status = SagaStatus.WaitingForEvent;
    await _sagaRepository.SaveAsync(sagaState, cancellationToken);
    return SagaResult.Success(sagaId);
}
```

**Why this is good:** Handles real-world async integrations (payment webhooks, shipping callbacks) without blocking.

### 5. Proper Compensation Logic

```csharp
// ✅ Compensate in reverse order, continue on failure
foreach (var step in Steps.Reverse())
{
    if (!completedSteps.Contains(step.StepName)) continue;
    try
    {
        await step.CompensateAsync(data, context, cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to compensate step {StepName}. Continuing...");
        // Don't throw - continue compensating other steps
    }
}
```

**Why this is good:** Maximizes rollback success even when individual compensations fail.

### 6. Clean Layer Dependencies

```
Domain.csproj  → No external dependencies (pure .NET)
Application.csproj → Domain only
Infrastructure.csproj → Application + Domain
```

**Why this is good:** Dependency inversion is properly respected.

---

## Domain Layer Analysis

### Order Entity

**Location:** `src/Domain/Entities/Order.cs`

#### Issue 1: Dual Event Collections (Confusing)

```csharp
// From AggregateRoot<T>
private readonly List<IDomainEvent> _domainEvents = new();

// In Order class  
private readonly List<IDomainEvent> _uncommitedEvents = new();
```

**Problem:** Two separate event collections exist. `_domainEvents` from the base class is never used.

**Impact:** Code readers will be confused about which collection to use.

**Fix:** Remove the inherited `AggregateRoot` events or consolidate.

#### Issue 2: God Aggregate (SRP Violation)

The Order aggregate handles **two distinct business processes**:

1. **Order Lifecycle:** Create → Pay → Approve → Complete → Cancel
2. **Return Lifecycle:** Request → Receive → Refund → Complete

```csharp
// Order creation responsibilities
public static Order Create(...) { }
public void Pay(PaymentId paymentId) { }
public void Approve() { }
public void Complete() { }
public void Cancel(...) { }

// Return responsibilities (should be separate aggregate)
public void RequestReturn(...) { }
public void ConfirmReturnReceived() { }
public void ProcessRefund(...) { }
public void CompleteReturn() { }
public void RevertReturnReceipt() { }
```

**Problem:** This creates a 475-line file with 13+ domain events, making it hard to:
- Understand state transitions
- Write focused unit tests
- Scale the team (two people can't work on this simultaneously)

**Industry Standard:** In FAANG systems, `Return` or `ReturnRequest` would be a **separate aggregate** in the same or different bounded context.

#### Issue 3: Unused/Dead Order Statuses

```csharp
public enum OrderStatus
{
    Pending = 0,
    AwaitingPayment = 1,  // ⚠️ Never set - no transition creates this
    Paid = 2,
    Approved = 3,
    Cancelling = 4,       // ⚠️ Never set - no transition creates this
    Cancelled = 5,
    Completed = 6,
    ReturnRequested = 7,
    ReturnReceived = 8,
    Refunded = 9,
    Returned = 10
}
```

**Problem:** Dead code implies incomplete implementation or abandoned features.

#### Issue 4: DateTime Inconsistency

```csharp
// In RequestReturn method
var orderAge = DateTime.Now - _completedAt.Value;  // ⚠️ Uses local time

// In Apply methods
_updatedAt = evt.CompletedAt;  // ✅ Event uses UtcNow

// In SagaBase
sagaState.UpdatedAt = DateTime.Now;  // ⚠️ Mixed with UtcNow elsewhere
```

**Problem:** Comparing local time with UTC time causes incorrect calculations, especially for users in different time zones.

### Value Objects Assessment

| Value Object | Assessment | Notes |
|--------------|------------|-------|
| `Money` | ✅ Excellent | Immutable, validated, arithmetic methods |
| `Address` | ✅ Good | Factory method, proper validation |
| `OrderId` | ✅ Good | Type-safe wrapper around Guid |
| `OrderStatus` | ⚠️ Issues | Contains unused values, should be smarter |

**Recommendation:** Consider making `OrderStatus` a smart enum with allowed transitions:

```csharp
public sealed record OrderStatus
{
    public static readonly OrderStatus Pending = new("Pending", Paid, Cancelled);
    public static readonly OrderStatus Paid = new("Paid", Approved);
    
    public string Name { get; }
    public IReadOnlyList<OrderStatus> AllowedTransitions { get; }
    
    public bool CanTransitionTo(OrderStatus target) => AllowedTransitions.Contains(target);
}
```

---

## Application Layer Analysis

### Saga Architecture

**Strengths:**
- Step ordering is explicit (`int Order`)
- Context accumulates data across steps
- Repository tracks saga state and step logs

**Weaknesses:**

#### Issue 1: Steps Contain Infrastructure Code

```csharp
// In CreateShipmentStep.ExecuteAsync
await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
// ... do stuff ...
await orderRepository.AddAsync(order, cancellationToken);
foreach (var domainEvent in order.UncommitedEvents)
{
    await outboxRepository.AddAsync(/*...*/);
}
await unitOfWork.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

**Problem:** Every step that modifies Order duplicates this exact pattern. Steps should orchestrate business logic, not manage persistence.

**Solution:** Extract to a `SagaOrderPersistenceService`:

```csharp
public interface ISagaOrderPersistenceService
{
    Task<Order?> LoadOrderAsync(Guid orderId, CancellationToken ct);
    Task SaveOrderAsync(Order order, CancellationToken ct);
}
```

#### Issue 2: Missing Idempotency in Steps

```csharp
public async Task<StepResult> ExecuteAsync(...)
{
    // ⚠️ No check if payment already exists!
    var paymentId = await paymentGateway.ProcessPaymentAsync(...);
    context.PaymentId = paymentId;
}
```

**Problem:** If the step fails after `ProcessPaymentAsync` but before persisting, retry will charge the customer twice.

**Solution:**

```csharp
public async Task<StepResult> ExecuteAsync(...)
{
    // ✅ Idempotency check
    if (!string.IsNullOrEmpty(context.PaymentId))
    {
        _logger.LogInformation("Payment already processed, skipping");
        return StepResult.SuccessResult();
    }
    
    var paymentId = await paymentGateway.ProcessPaymentAsync(...);
    context.PaymentId = paymentId;
}
```

#### Issue 3: String-Based Step Resolution

```csharp
var resumeStep = Steps.FirstOrDefault(s => s.StepName == fromStepName);
```

**Problem:** Typos in step names fail silently at runtime.

**Better:** Use strongly-typed step identifiers or enum.

### Command Handler Assessment

```csharp
// CreateOrderCommandHandler
public async Task<Result<Guid>> Handle(CreateOrderCommand request, ...)
{
    // ✅ Good: Transaction wrapping
    // ✅ Good: Outbox pattern
    // ⚠️ Missing: Duplicate check using IdempotencyKey
}
```

**The `IdempotencyKey` in the command is never used!**

---

## Infrastructure Layer Analysis

### Kafka Configuration

```csharp
var config = new ProducerConfig
{
    BootstrapServers = "localhost:9092",  // ⚠️ Hardcoded
    EnableIdempotence = true,             // ✅ Good
    MaxInFlight = 1,                      // ⚠️ Limits throughput
    Acks = Acks.All,                      // ✅ Good
    MessageSendMaxRetries = 10            // ✅ Good
};
```

**Issues:**
- Hardcoded connection string (should use configuration)
- `MaxInFlight = 1` severely limits throughput (typical production: 5)

### Saga Repository

```csharp
public async Task SaveAsync(SagaState sagaState, CancellationToken cancellationToken)
{
    // ...
    //@think: debatable
    // await dbContext.SaveChangesAsync(cancellationToken);  // Commented out!
}
```

**Problem:** Repository doesn't actually save unless caller also calls `SaveChangesAsync`. This breaks repository abstraction.

### Outbox Processor

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        var messages = await outboxRepo.GetUnprocessedMessagesAsync(20, stoppingToken);
        foreach (var message in messages)
        {
            // ⚠️ Sequential processing - slow!
        }
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
    }
}
```

**Issues:**
- Sequential processing of messages (should parallelize)
- No maximum message age handling
- No dead letter queue for poison messages

---

## Code Smells & Antipatterns

### 1. Primitive Obsession (Minor)

```csharp
// In SagaData
public Guid CorrelationId { get; set; }  // Should be OrderId value object
public Guid CustomerId { get; set; }      // Should be CustomerId value object
```

### 2. Magic Strings

```csharp
sagaState.Context = JsonSerializer.Serialize(context);  // No type safety
var result = await _producer.ProduceAsync("order.events", ...);  // Topic name hardcoded
```

### 3. Comments as Code Smell

```csharp
// @think: should we create saga/sagaStep domain entity with 
// additional logic for state changing type shit
// UPD: We AGNI, but comment will remain in honor to anti-AI development
```

These indicate unresolved design decisions.

### 4. Inconsistent Error Handling

```csharp
// CreateShipmentStep
catch (InvalidAddressException ex)
{
    _logger.LogInformation(ex, ...);  // ⚠️ LogInformation for error?
}
catch (Exception ex)
{
    _logger.LogWarning(ex, ...);      // ⚠️ LogWarning for general exception?
}
```

### 5. Unused Builder Pattern

```csharp
// OrderItem.cs
public static class Builder
{
    public static OrderItem Build(ProductId productId, int quantity, Money price)
    {
        return Create(productId, quantity, price);  // Just calls Create!
    }
}
```

**Problem:** Builder adds no value, just indirection.

---

## FAANG Comparison: How Big Tech Handles This

### Google/Meta Approach to Sagas

| Aspect | Your Implementation | FAANG Standard |
|--------|-------------------|----------------|
| **State Machine** | String-based step names | Explicit state machine with typed transitions (e.g., Temporal.io, AWS Step Functions) |
| **Observability** | Logging only | Structured logging + Metrics + Traces (OpenTelemetry) |
| **Circuit Breaker** | None | Polly / built-in circuit breaker for all external calls |
| **Dead Letter Queue** | None | DLQ with alerting and manual retry UI |
| **Saga Timeout** | `GetStuckSagasAsync` exists but unused | Active timeout handler with auto-compensation |
| **Event Versioning** | None | Schema Registry + Event Upcasters |
| **Idempotency** | Partial (saga level) | Full (every step, every API call) |
| **Distributed Tracing** | None | Correlation ID propagated through Kafka headers to all services |

### Uber/Netflix Approach to Order Domain

They would typically split this into:
1. **Order Service** - Core order lifecycle
2. **Return Service** - Separate bounded context
3. **Payment Service** - Handles payment orchestration
4. **Inventory Service** - Stock management

Your Order aggregate doing returns is like having your `UserService` also handle password reset flows - technically possible, but not scalable organizationally.

### What FAANG Would Add

1. **Feature Flags** - Ability to disable return flow without deployment
2. **A/B Testing** - Different return windows for different customer segments
3. **Rate Limiting** - Prevent abuse
4. **Audit Log** - Complete history of who did what when
5. **Admin Tools** - Manual intervention for stuck sagas
6. **Health Checks** - Kafka lag, stuck saga count, outbox queue depth

---

## Detailed Recommendations

### High Priority

#### 1. Split Order and Return Aggregates

```csharp
// New aggregate
public sealed class ReturnRequest : AggregateRoot<ReturnRequestId>
{
    public OrderId OrderId { get; }
    public CustomerId CustomerId { get; }
    public ReturnStatus Status { get; }
    public Money RefundAmount { get; }
    
    public void Approve() { }
    public void ReceiveItems() { }
    public void ProcessRefund() { }
    public void Complete() { }
}
```

#### 2. Add Step Idempotency

```csharp
public interface ISagaStep<TData, TContext>
{
    // Add this
    bool ShouldSkip(TData data, TContext context);
}

// In each step
public bool ShouldSkip(OrderSagaData data, OrderSagaContext context)
    => !string.IsNullOrEmpty(context.PaymentId);
```

#### 3. Extract Order Persistence from Steps

```csharp
public class SagaOrderService(
    IOrderRepository orderRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<T> ExecuteWithOrderAsync<T>(
        Guid orderId,
        Func<Order, Task<T>> action,
        CancellationToken ct)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
        var order = await orderRepository.GetByIdAsync(OrderId.From(orderId), ct);
        var result = await action(order);
        // ... save order, outbox, commit
        return result;
    }
}
```

### Medium Priority

#### 4. Fix DateTime Consistency

```csharp
public interface ISystemClock
{
    DateTime UtcNow { get; }
}

// Use everywhere instead of DateTime.Now/UtcNow
```

#### 5. Add Circuit Breaker

```csharp
// Use Polly
services.AddHttpClient<IPaymentGateway>()
    .AddPolicyHandler(Policy
        .Handle<HttpRequestException>()
        .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1)));
```

#### 6. Implement Dead Letter Queue

```csharp
catch (Exception ex) when (message.RetryCount >= 5)
{
    await deadLetterQueue.SendAsync(message, ex);
    await outboxRepo.MarkAsFailedAsync(message.Id, ct);
}
```

### Low Priority

#### 7. Remove Unused Code

- Delete `OrderStatus.AwaitingPayment`
- Delete `OrderStatus.Cancelling`
- Delete `OrderItem.Builder` class
- Consolidate event collections in `Order`

#### 8. Add Distributed Tracing

```csharp
// In Kafka producer
Headers = new Headers
{
    { "trace-id", Activity.Current?.Id },
    { "span-id", Activity.Current?.SpanId.ToString() }
}
```

---

## Priority Action Items

| Priority | Item | Effort | Impact |
|----------|------|--------|--------|
| 🔴 P0 | Add idempotency checks to saga steps | Medium | Prevents double-charging |
| 🔴 P0 | Fix DateTime inconsistency | Low | Prevents time-based bugs |
| 🟠 P1 | Extract persistence from saga steps | Medium | Reduces code duplication |
| 🟠 P1 | Split Order and Return aggregates | High | Improves maintainability |
| 🟡 P2 | Add circuit breaker for external calls | Low | Improves resilience |
| 🟡 P2 | Implement dead letter queue | Medium | Improves operability |
| 🟢 P3 | Remove unused code | Low | Reduces confusion |
| 🟢 P3 | Add distributed tracing | Medium | Improves debugging |

---

## Conclusion

This codebase shows **strong architectural foundations** - the choice of event sourcing, saga pattern, and transactional outbox demonstrates good understanding of distributed systems. The code is generally well-organized and follows DDD principles.

**Key Strengths:**
- Proper event sourcing in Order aggregate
- Well-designed value objects
- Resumable saga with compensation
- Clean layer separation

**Key Weaknesses:**
- Order aggregate handles too much (order + returns)
- Saga steps contain infrastructure concerns
- Missing idempotency at step level
- No circuit breaker or DLQ

**For production readiness at scale**, focus on:
1. Idempotency everywhere
2. Splitting the Order aggregate
3. Adding operational tooling (metrics, DLQ, admin UI)

The saga architecture is solid and comparable to what you'd see in mid-tier tech companies. To reach FAANG level, you'd want to consider Temporal.io or similar workflow engines that handle many of these concerns out of the box.

---

*Review completed by architectural analysis.*

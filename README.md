# allah

2 apps, one extremely complicated ebay with crypto support + public api, and second one is less complicated but also stupidly complex xiaoping-express

ebay stack:

Tech info (not complete)
user service

user service probably should be a rest api, but 90% of other microservices use grpc and don't expose any methods for public, all use gateway, so to make it consistent
i prefer consistency to logic. user-service will be grpc microservice, yes it's overkill, but zhizn' igra, igray krasivo


auth service

some auth stuff. also grpc

order service:
i am idiot so i admire complexity: saga based transaction with sprinkle of outbox transactions, kafka workers, ddd aggregates, event sourcing and cqrs. 

8 fucking layers to create order

Internal IDs: Use Guid (OrderId, CustomerId, ProductId)
External IDs: Use string (PaymentId, TrackingId, ShipmentId, RefundId)

// ❌ BAD: Step modifies saga state directly
public async Task<StepResult> ExecuteAsync(...)
{
await RegisterWebhook();

    // Step shouldn't do this!
    sagaState.Status = SagaStatus.WaitingForEvent;  
    
    return StepResult.Success();
}

// ✅ GOOD: Step communicates via result
public async Task<StepResult> ExecuteAsync(...)
{
await RegisterWebhook();

    // Step signals its intent
    return StepResult.SuccessResult(new Dictionary {
        ["SagaState"] = "WaitingForEvent"
    });
}
```

### Metadata is a Communication Protocol
```
┌─────────────────┐
│  Step (Child)   │
│                 │
│  "Hey parent,   │
│  I need to wait │
│  for webhook"   │
└────────┬────────┘
│ Metadata
│ ["SagaState"] = "WaitingForEvent"
▼
┌─────────────────┐
│ SagaBase (Parent│
│                 │
│ "OK, I'll pause │
│ the saga for you│
└─────────────────┘

for direct approach step whould need access to saga state, and this violates all shit
using metadata we signal about shit and saga base class will handle thi shit - event driven saga





## TODO:

IDeadLetterRepository - add implementation
ReturnPolicyService - implement
unitOfWork.CommitAsync() - add method
SagaRepository - add all methods
createManualReviewStepForReturn - add logic


Isolation in domain entity: 

Orders over $1000 require manual approval

Year 2024: You put this check inside the Apply method. You save an order for $1200.

Year 2025: The business changes the rule: "Orders over $500 require manual approval." You update the code.

The Disaster: You try to load that $1200 order from 2024. The Apply method runs, sees $1200, checks the new $500 limit, finds it wasn't "approved" under the new rules, and throws an error. Your historical data is now unreadable.

By isolating the state change, you ensure that once an event is written to the store, the Apply method will always be able to rebuild that state, regardless of how business rules change in the future.


# Gabriela

Plan: 2 apps, one extremely complicated microservice zoo ebay with crypto support + public api, and second one is less complicated but also stupidly complex xiaoping-express

ebay stack:

Tech info (not complete)
user service

user service probably should be a rest api, but 90% of other microservices use grpc and don't expose any methods for public, all use gateway, so to make it consistent
i prefer consistency to logic. user-service will be grpc microservice, yes it's overkill, but zhizn' igra, igray krasivo


auth service

some auth stuff. simple as fuck

order service:
saga based transaction with sprinkle of outbox transactions, kafka workers, ddd aggregates, distributed locking, watchdogs, event sourcing and cqrs, and ALMOST exactly-once processing
also suppoting b2b orders and subscription (recurring order)

supraphysiological amount of lean unit tests, also some integration and e2e tests type shit


product service: 
internal kafka cqrs service. used by order (kafka) + REST APIcko for product-guy 

AI: 
...to be cont

Internal IDs: Strongly typed Ids based on Guid (OrderId, CustomerId, ProductId)
External IDs: Use string (PaymentId, TrackingId, ShipmentId, RefundId)

We use Metadata object in saga step result as communication protocol so we can
communicate between steps and saga without need access to saga state (which is violation).
 => Step (child) => we need to wait for webhook, sir
 => Saga () => i will pause for you next step, my boy

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
```

for direct approach step should need access to saga state, and this violates all shit
using metadata we signal about shit and saga base class will handle thi shit - event driven saga


Isolation in domain entity: 

Orders over $1000 require manual approval

Year 2024: You put this check inside the Apply method. You save an order for $1200.

Year 2025: The business changes the rule: "Orders over $500 require manual approval." You update the code.

The Disaster: You try to load that $1200 order from 2024. The Apply method runs, sees $1200, checks the new $500 limit, finds it wasn't "approved" under the new rules, and throws an error. Your historical data is now unreadable.

By isolating the state change, you ensure that once an event is written to the store, the Apply method will always be able to rebuild that state, regardless of how business rules change in the future.


T0: Customer in EU clicks "Place Order"
→ Request goes to EU-WEST-1
→ Network latency to US (300ms)

T1 (100ms): EU processes request
OrderCreatedEvent { OrderId: <guid-A>, CustomerId: 123, Items: [ProductA] }
→ Starts replicating to US...

T2 (200ms): Customer gets impatient, clicks again
→ Browser sends ANOTHER request
→ Round-robin load balancer routes to US-EAST-1 (because EU is "slow")

T3 (250ms): US processes SECOND request (doesn't know about first)
OrderCreatedEvent { OrderId: <guid-B>, CustomerId: 123, Items: [ProductA] }
→ Starts replicating to EU...

T4 (500ms): Both regions see TWO orders from same customer
❓ Which one is real? Did customer intend 1 order or 2?


it's still one method for everything, but we don't use dispatchProxy via reflection 
which is more voodoo than Reagan economic type shit

saga handler it's just listen for kafka and run executeASync for specific saga (sagaBase method which is already part of every saga because it's abstract class)

Integration tests only (don't mock EF)
File	Why
OrderPersistenceService	EF transactions + ExecutionStrategy + snapshot/event store interactions. Mocking all of this gives zero confidence. Needs a real Postgres (Testcontainers).
ReturnRequestPersistenceService	Same reason as above.
OrderReadModelUpdater	AnyAsync, SaveChangesAsync, dbContext.OrderReadModels.Add — the value of the test is in verifying the SQL side. Use Testcontainers + real EF migrations.
ReturnRequestReadModelUpdater	Same as above.
EventIdempotencyChecker	The main interesting case is the duplicate key race condition path — that's a DB-level concern. Unit test with EF in-memory could be faked but misses the real uniqueness constraint.
ProcessedEventsCleanupService	ExecuteDeleteAsync batch loop — this is entirely about DB behavior.
KafkaReadModelSynchronizer	Builds its own Kafka consumer in the constructor (hardcoded new ConsumerBuilder), reflection-based event type discovery. Can't be unit tested meaningfully without a testable constructor. Needs an integration test with Testcontainers Kafka. This is also the file with your own @think: too much voodoo here comment — agreed.

we use lookup repository specificly for returnRequest, because we need strict consistency during command processing. with orders we dont need it so we have just read repo for query handlers

FUCKING SYNCHRONIZATION WITH READ MODEL

1. PersistenceService (same DB transaction)
   ├── DomainEvents table
   ├── OutboxMessages table
   └── [ReturnRequest only] ReturnRequestLookups table

2. OutboxProcessor (background service, polls every ~2s)
   └── reads unprocessed OutboxMessages → publishes to Kafka

3. KafkaReadModelSynchronizer (background service, Kafka consumer)
   └── consumes from "order.events" / "return.events"
       ├── OrderReadModelUpdater.HandleAsync(...)   → OrderReadModels table
       └── ReturnRequestReadModelUpdater.HandleAsync(...) → ReturnRequestReadModels table
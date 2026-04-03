# Inventory Service

owns real-time stock state and reservation lifecycle used by Order saga. It is a transactional write service.

we have complex InventoryReservationStore, but

we wrap each exposed api method into "transaction-retry engine" with fresh dbContext.

BECAUSE:  this code is retrying the whole operation after a failed transactional attempt, A 40001 error means the database is saying:  transaction snapshot conflicted with another transaction, throw this attempt away and run it again against fresh committed state.

why old dbContext is shitty idea: 
- it still has tracked entities from the failed attempt
- it still has original values and assumptions from the old snapshot
- it may still hold pending changes built from stale reads

but fresh dbContext is forcing to: 
- fresh reads
- fresh tracking
- fresh transaction
- fresh business decision based on current committed rows

First rule of overengineering:
Normal request, one query or one SaveChanges, no retry: same scoped DbContext is completely normal.
Retrying a whole EF Core unit of work after a failed transactional attempt: fresh DbContext is the safer pattern.
Plain raw SQL or stateless command retry: you do not automatically need a fresh DbContext, because there may be no meaningful tracked EF state to throw away.
So the fresh DbContext is not really about transaction by itself. It is about rerunning a failed unit of work correctly.

Mental healh formula:

Request
-> retry wrapper
-> new DbContext
-> new transaction
-> run reserve/confirm/release logic
-> commit

If conflict 40001:
-> rollback
-> discard old DbContext
-> create new DbContext
-> rerun logic
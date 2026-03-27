# Vector Indexer Worker

Consumes product events from Kafka → builds text corpus → calls EmbeddingService → upserts into Qdrant.

- no http, no grpc, just a long-running async kafka consumer
- `auto.offset.reset: latest` - won't replay history on (re)start, only new events
- manual commit after processing — at-least-once delivery, not exactly-once

## Topics

Explicit list, no wildcard:
- `product.created` → upsert
- `product.updated` → upsert (same path, just overwrites)
- `product.deleted` → delete from Qdrant

Event type comes from the **Kafka message header** (`EventType`), not the topic name

```
"name | description | category | key: value | key: value"
```

That string gets embedded and stored in Qdrant with a payload (price, color, brand, etc) for filtering
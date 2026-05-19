---
applyTo: "AI/UserPreferenceWorker/**"
description: "Use when working on the User Preference Worker — Kafka consumer that aggregates user behavioral events (views, clicks, purchases) into per-user preference profiles stored in Redis, and tracks purchase co-occurrences for collaborative filtering."
---

# User Preference Worker

## Overview

Long-running worker that consumes user behavioral events from Kafka, computes weighted preference profiles with time decay, tracks purchase co-occurrences for "frequently bought together" recommendations, and stores everything in Redis. Pure aggregation service — no AI, no vectors, no gRPC server.

## Architecture

- **Kafka consumer** (confluent-kafka) — subscribes to `user.events` topic
- **Redis storage** (redis.asyncio) — interaction lists + computed profiles
- No HTTP server, no gRPC server — pure background worker

## Event Handling

Event type comes from Kafka message header (`event-type`), dispatched via `match/case`:

| Event | Action | Base Weight |
|-------|--------|-------------|
| `ProductViewed` | Record view interaction, boost for long duration (>10s up to 2x) | 1.0 |
| `ProductClicked` | Record click, boost for high rank (>5 position → 1.5x) | 2.0 |
| `PurchaseCompleted` | Record purchase + record co-occurrences via `CoOccurrenceTracker` | 5.0 |
| `SearchBounced` | Log only (negative signal, not aggregated yet) | — |

## Event Source

Events are published by Gateway REST endpoints (`POST /api/v1/user-events/*`) to the `user.events` Kafka topic via `KafkaUserEventPublisher`. The frontend calls these endpoints on user actions.

## Tech Stack & Conventions

- Python 3.10+, async event loop with blocking Kafka poll offloaded to thread pool
- **Dependencies**: confluent-kafka, redis, pydantic, pydantic-settings, structlog
- **Config**: `pydantic-settings` with `env_prefix="USER_PREF_"` in `config.py`
- **Logging**: `structlog` — use `structlog.get_logger()`, log with key-value pairs
- **Models**: Pydantic `BaseModel` in `models.py` — event models + `UserPreferenceProfile`

## Code Patterns

- `consumer.py` — Kafka consumer loop with `run_in_executor` for non-blocking poll, manual commit after processing (at-least-once), `process_event()` as testable entry point; receives both `aggregator` and `cooccurrence` as parameters
- `aggregator.py` — `PreferenceAggregator` class: records interactions to Redis list (LPUSH + LTRIM), recomputes full profile on every interaction
- `cooccurrence.py` — `CoOccurrenceTracker` class: on each purchase, pairs new item with recent purchases using bidirectional ZINCRBY on Redis sorted sets; `get_co_occurrences()` returns top co-occurring items
- `models.py` — `EventType` string constants, per-event Pydantic models, `UserPreferenceProfile` output model
- `config.py` — `Settings` with Kafka, Redis, and aggregation params
- `main.py` — creates both `PreferenceAggregator` and `CoOccurrenceTracker`, passes both to `run_consumer()`
- Pattern matching (`match/case`) for event type dispatch (same pattern as VectorIndexerWorker)

## Aggregation Logic

- Interactions stored as JSON list per user in Redis, capped at `max_interactions_per_user` (default 200)
- Exponential time decay with configurable half-life (default 14 days): `weight * exp(-0.693 * age / half_life)`
- Profile recomputed on every new interaction (not batched)
- Top N categories/brands kept (default 10 each)
- Price percentiles: p25 and p75 of interacted prices (user's comfort price range)
- Profiles expire after 30 days of no activity (Redis TTL)

## Redis Keys

- `user:{user_id}:interactions` — list of last N interaction JSON objects
- `user:{user_id}:preference_profile` — computed profile JSON with 30-day TTL
- `user:{user_id}:recent_purchases` — list of last 50 purchased catalog item IDs (90-day TTL), used by co-occurrence tracker
- `cooccurrence:purchase:{catalog_item_id}` — sorted set of co-purchased item IDs with frequency scores (180-day TTL)

## Testing

- Framework: pytest + pytest-asyncio (asyncio_mode = "auto")
- Redis mocking: `fakeredis.aioredis.FakeRedis`
- Test layout: `tests/test_aggregator.py`, `tests/test_consumer.py`, `tests/test_cooccurrence.py`
- `conftest.py` adds project root to `sys.path`
- Aggregator tests use real fakeredis instances, consumer tests use `AsyncMock` aggregator
- Tests are plain `async def test_*` functions — no class-based tests

## Key Rules

- `auto.offset.reset: latest` — won't replay history on restart, only new events
- No gRPC, no HTTP endpoints — this is a pure consumer/writer
- Redis is the only datastore — no Postgres, no Elasticsearch, no Qdrant
- Profile is fully recomputed on every event, not incrementally patched
- Aggregator must always be instantiated with an async Redis client

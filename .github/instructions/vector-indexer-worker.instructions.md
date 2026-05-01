---
applyTo: "AI/VectorIndexerWorker/**"
description: "Use when working on the Vector Indexer Worker — Kafka consumer that embeds product events via gRPC and upserts vectors into Qdrant."
---

# Vector Indexer Worker

## Overview

Long-running worker that consumes product events from Kafka, builds a text corpus from each product, gets embeddings via gRPC streaming from EmbeddingService, and upserts vectors + payload into Qdrant.

## Architecture

- **Kafka consumer** (confluent-kafka) — subscribes to `product.events` topic
- **gRPC streaming client** — single persistent bidirectional stream to EmbeddingService (port 50052)
- **Qdrant client** — upserts/deletes vectors with metadata payload
- No HTTP server — this is a pure background worker

## Event Handling

Event type comes from Kafka message header (`event-type` or `EventType`), not topic name:

| Event | Action |
|-------|--------|
| `ProductCreateEvent` / `ProductUpdatedEvent` | Build corpus → embed → upsert to Qdrant |
| `ProductStockUpdatedEvent` | Update payload only (stock_quantity + status) — no re-embedding |
| `ProductDeletedEvent` | Delete point from Qdrant |

Corpus format: `"name | description | category | key: value | key: value"`

## Tech Stack & Conventions

- Python 3.10+, async event loop with blocking Kafka poll offloaded to thread pool
- **Dependencies**: confluent-kafka, grpcio, httpx, pydantic, pydantic-settings, qdrant-client, structlog
- **Config**: `pydantic-settings` with `env_prefix="INDEXER_"` in `config.py`
- **Logging**: `structlog` — use `structlog.get_logger()`, log with key-value pairs
- **Models**: Pydantic `BaseModel` in `models.py` — `ProductEvent`, `ProductStockUpdatedEvent`, `ProductAttribute`
- **Generated code**: `embedding_pb2.py` / `embedding_pb2_grpc.py` — do NOT edit manually

## Code Patterns

- `consumer.py` — Kafka consumer loop with `run_in_executor` for non-blocking poll, manual commit after processing (at-least-once)
- `indexer.py` — `Indexer` class with `upsert()`, `delete()`, `update_stock()` — orchestrates embedding + Qdrant writes
- `build_product_corpus(event)` — pure function that concatenates product fields into a single embeddable string
- `grpc_embedding_client.py` — `GrpcEmbeddingClient` with auto-reconnect (one retry on stream error)
- `qdrant_client.py` — `QdrantIndexClient` wraps qdrant-client with `ensure_collection()`, `upsert()`, `delete()`, `update_payload()`
- Pattern matching (`match/case`) for event type dispatch

## Testing

- Framework: pytest + pytest-asyncio (asyncio_mode = "auto")
- HTTP mocking: `respx`
- Test layout: `tests/unit/`, `tests/integration/`, `tests/e2e/`
- Integration tests use `testcontainers` for Qdrant
- Unit tests mock `embedding` and `qdrant` as `AsyncMock` injected into `Indexer`
- Tests are plain `async def test_*` or sync `def test_*` functions — no class-based tests

## Key Rules

- `auto.offset.reset: latest` — won't replay history on restart, only new events
- Manual commit — at-least-once delivery (upsert is idempotent so duplicates are safe)
- gRPC stream reconnects automatically once; if second attempt fails, exception propagates
- Vector dimensions are 768 (nomic-embed-text) — configured in settings, must match Qdrant collection
- Qdrant point ID is the `product_id` string — ensures upsert overwrites correctly
- Blocking `consumer.poll()` runs in thread executor to avoid blocking asyncio event loop

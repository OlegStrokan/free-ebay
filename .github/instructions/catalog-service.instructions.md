---
applyTo: "Catalog/**"
description: "Use when working on the Catalog Service — Kafka consumer that denormalizes Product events into Elasticsearch with failure classification, retry store, and DLQ handling."
---

# Catalog Service

## Overview

Event-driven consumer service (no synchronous API beyond health checks). Subscribes to Product events from Kafka and maintains a denormalized Elasticsearch index for the Search service. Implements failure classification with a PostgreSQL-backed retry store.

## Architecture (Consumer Worker Pattern)

- **Api/** — Minimal: Program.cs with health checks only
- **Application/** — Event consumers (per event type), search document models, `IElasticsearchIndexer`, retry store abstractions, `ApplicationModule.cs`
  - **Consumers/** — `ProductCreatedConsumer`, `ProductUpdatedConsumer`, `ProductDeletedConsumer`, `ProductStockUpdatedConsumer`, `ProductStatusChangedConsumer`
  - **Models/** — `ProductSearchDocument`, `StockUpdateDocument`, `StatusUpdateDocument`, `ProductFieldsUpdateDocument`
  - **RetryStore/** — `IRetryStore`, `RetryRecord`
- **Infrastructure/** — Kafka consumer, Elasticsearch indexer, retry store implementation, `InfrastructureModule.cs`
  - **Elasticsearch/** — `ElasticsearchIndexer`, `ElasticsearchIndexInitializer`, `ElasticsearchOptions`
  - **Kafka/** — `KafkaConsumerBackgroundService`, `RetryWorkerBackgroundService`, `FailureClassifier`, `EventWrapper`
  - **RetryStore/** — `PostgresRetryStore`, `RetryStoreInitializer`

## Tech Stack & Conventions

- .NET 8, ASP.NET Core (health checks only)
- Kafka: Confluent.Kafka (consumer)
- Elasticsearch 8.17
- PostgreSQL (retry store, no EF Core — direct Npgsql)
- Observability: OpenTelemetry
- Testing: NUnit + NSubstitute (unit), integration with Testcontainers
- Module pattern: `AddApplicationServices()`, `AddInfrastructureServices()`

## Event Flow

1. Product Service publishes to `product-events` Kafka topic
2. `KafkaConsumerBackgroundService` receives event (wrapped in `EventWrapper`)
3. Routes to appropriate `IProductEventConsumer` by `EventType` property
4. Consumer updates Elasticsearch index (full upsert or partial update)
5. On failure: `FailureClassifier` categorizes the error

## Failure Classification & Retry

- **Systemic failures**: Connection errors, timeouts, HTTP 5xx, 429 (rate limiting) → retry with exponential backoff
- **Message-specific failures**: Data validation, malformed events → DLQ after retries exhausted
- **PostgresRetryStore**: Tracks failed messages with retry count, last error, timestamp
- **RetryWorkerBackgroundService**: Periodically reprocesses failed messages
- **RetryStoreInitializer**: Creates retry table on startup

## Code Patterns

- **Consumer interface**: `IProductEventConsumer` with `EventType` property and `ConsumeAsync()` method
- **Event deserialization**: `JsonSerializer.Deserialize<PayloadType>()` from `EventWrapper.Payload`
- **Search documents**: Flat structures matching Elasticsearch mapping (not domain entities)
- **Partial updates**: Stock and status changes update specific fields, not the full document
- **Index initialization**: `ElasticsearchIndexInitializer` ensures index exists on startup

## Configuration

- `KafkaOptions`: Bootstrap servers, topic, consumer group
- `ElasticsearchOptions`: URL, index name
- `RetryStoreOptions`: Connection string, max retries
- OpenTelemetry Jaeger endpoint

## Testing

- **Unit**: Consumer logic with mocked `IElasticsearchIndexer` — verify upsert calls and document shapes
- **Integration**: Full Kafka consumer with Elasticsearch via Testcontainers
- Test focus: consumer routing, retry classification, document mapping

## Key Rules

- Catalog does NOT own Product data — it only denormalizes from events
- Elasticsearch documents are flat search-optimized structures, not domain models
- Partial updates (stock, status) must not overwrite the full document
- Failure classification determines retry vs DLQ — don't retry message-specific errors indefinitely
- Index must exist before consuming — `ElasticsearchIndexInitializer` handles this at startup

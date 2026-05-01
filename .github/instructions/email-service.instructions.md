---
applyTo: "Email/**"
description: "Use when working on the Email Service — Kafka consumer worker that sends transactional emails via SMTP with idempotency, retry logic, and DLQ handling."
---

# Email Service

## Overview

.NET Worker Service (not web) that consumes email events from Kafka and sends transactional emails via SMTP. Implements idempotency, retry policies, and dead-letter queue (DLQ) replay.

## Architecture (Worker Pattern — NOT layered)

- **Messaging/** — `KafkaEmailConsumer` (BackgroundService), `KafkaEmailDlqReplayWorker`, `EventWrapper`
- **Services/** — `SmtpEmailSender` (IEmailSender), `PostgresProcessedMessageStore` (idempotency)
- **Models/** — Event DTOs: `AuthEmailRequested`, `OrderConfirmationEmailRequested`
- **Options/** — `KafkaOptions`, `EmailDeliveryOptions` (SMTP config)

## Tech Stack & Conventions

- .NET 8 Worker (Microsoft.Extensions.Hosting) — no HTTP, no gRPC
- PostgreSQL via Npgsql (direct SQL, no EF Core)
- Kafka: Confluent.Kafka (consumer + producer for DLQ)
- Email: System.Net.Mail (SMTP)
- Logging: structured via ILogger
- Classes are `sealed` — no inheritance expected

## Code Patterns

- **Event-driven**: BackgroundService polls Kafka, dispatches by event type
- **EventWrapper pattern**: Deserializes Kafka messages with metadata (EventId, EventType, Payload)
- **Idempotency**: `PostgresProcessedMessageStore` tracks EventIds — prevents duplicate sends
- **Retry policy by importance**:
  - `Important=true`: Full retry (MaxDeliveryAttempts: 3), exponential backoff, DLQ on final failure
  - `Important=false`: Single attempt, no DLQ, marked as handled
- **DLQ replay**: `KafkaEmailDlqReplayWorker` re-publishes failed messages (configurable: `EnableDlqReplay`, `DlqReplayRunOnce`)
- **IOptions\<T\>** pattern for configuration binding
- Direct Kafka API (ConsumerBuilder/ProducerBuilder) — no abstraction layers

## Event Types

- `EmailVerificationRequested` — from Auth service
- `PasswordResetRequested` — from Auth service
- `OrderConfirmationEmailRequested` — from Order service

## Configuration

- `KafkaOptions`: BootstrapServers, topics, consumer group, MaxDeliveryAttempts, retry delays, DLQ settings
- `EmailDeliveryOptions`: SMTP host, port, SSL, credentials, default from address
- PostgreSQL connection string for processed message store

## Testing

- **Unit**: xUnit + NSubstitute
- Tests in `Email.Tests/` focused on Messaging layer
- Test patterns: message parsing, retry logic, DLQ behavior

## Key Rules

- Idempotency is mandatory — always check processed message store before sending
- Never retry unimportant messages — they get one shot
- DLQ replay can run once (batch mode) or continuously — controlled by config
- SMTP failures should not crash the worker — log and handle per retry policy
- EventId uniqueness is the deduplication key across all event types

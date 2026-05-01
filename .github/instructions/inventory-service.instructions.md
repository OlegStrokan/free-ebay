---
applyTo: "Inventory/**"
description: "Use when working on the Inventory Service — gRPC service managing stock levels and reservations with saga-compatible reserve/confirm/release/expire lifecycle, outbox pattern, and serializable retry logic."
---

# Inventory Service

## Overview

gRPC service that manages product stock quantities and reservations. Supports the order saga with reserve → confirm/release/expire lifecycle. Uses the transactional outbox pattern for reliable event publishing to Kafka.

## Architecture (Clean Architecture + Modules)

- **Api/** — gRPC service (`InventoryGrpcService`)
- **Application/** — `ApplicationModule.cs` registers services
  - **Interfaces/** — `IInventoryService`, `IInventoryReservationStore`
  - **Models/** — DTOs and result types (ReserveInventoryCommand, ReserveInventoryResult, etc.)
  - **Services/** — `InventoryService` (business logic, validation wrapper)
- **Infrastructure/** — `InfrastructureModule.cs`
  - **Persistence/** — `InventoryDbContext`, `InventoryReservationStore`, Entities, Enums
  - **Messaging/** — `KafkaOutboxPublisher`, `KafkaOptions`
  - **BackgroundServices/** — `OutboxProcessor`, `ReservationExpiryProcessor`
- **Protos/** — gRPC definitions

## Tech Stack & Conventions

- .NET 8, gRPC with health checks
- PostgreSQL + EF Core 8 with `DbContextFactory`
- Kafka producer (via outbox pattern)
- OpenTelemetry: AspNetCore + EF Core instrumentation, OTLP to Jaeger
- Primary constructor DI (C# 12)
- Sealed classes where inheritance not expected
- Module pattern: `AddApplicationServices()`, `AddInfrastructureServices(config)`

## Core Design Patterns

### Serializable Retry Pattern
`InventoryReservationStore` uses **fresh DbContext per retry** (max 3 attempts) on PostgreSQL 40001 serialization errors:
- Each failed attempt discards the DbContext with stale tracked entities
- Fresh DbContext gets a clean snapshot for correct re-execution
- Prevents corruption from retrying with stale Change Tracker state

### Outbox Pattern
- Side effects (inventory events) written to outbox table in the **same transaction** as stock changes
- `OutboxProcessor` (BackgroundService) polls and publishes to Kafka in batches
- Guarantees at-least-once event delivery without distributed transactions

### Result Objects (not exceptions)
- Business failures return explicit result types with failure reasons
- `ReserveInventoryFailureReason` enum: Validation, ProductNotFound, InsufficientStock
- Exceptions reserved for infrastructure failures only

## Database Schema

- **product_stocks** — available/reserved quantities with CHECK constraints (non-negative)
- **inventory_reservations** — reservation state (Active/Confirmed/Released/Expired)
- **inventory_reservation_items** — line items per reservation
- **inventory_movements** — audit trail of all stock movements (Reserve/Confirm/Release/Expire)
- **inventory_outbox_messages** — pending events for Kafka publishing

## Configuration

- PostgreSQL connection, Kafka settings, Outbox (BatchSize: 20, PollIntervalMs: 2000), ReservationExpiry TTL (30 min)
- OpenTelemetry Jaeger endpoint
- Health checks for k8s probes

## Testing

- **Unit**: xUnit + NSubstitute
- **Integration**: xUnit + Testcontainers.PostgreSql
- **E2E**: Full gRPC tests
- Focus areas: reservation state transitions, serializable retry behavior, outbox processing

## Key Rules

- Always use `DbContextFactory` for reservation operations — never reuse a scoped context across retries
- Stock changes and outbox writes MUST be in the same transaction
- Reservation expiry is 30 minutes — `ReservationExpiryProcessor` handles cleanup
- PostgreSQL 40001 (serialization failure) is expected under contention — retry, don't crash
- CHECK constraints on `product_stocks` are the last line of defense against negative stock
- Currently only reserve and release are called by Order saga — confirm/expire paths exist but are not externally invoked yet

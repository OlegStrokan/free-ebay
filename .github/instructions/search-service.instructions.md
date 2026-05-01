---
applyTo: "Search/**"
description: "Use when working on the Search Service — gRPC query service with hybrid Elasticsearch + AI search, fallback strategy, and progressive streaming results."
---

# Search Service

## Overview

Read-only gRPC service that provides product search. Supports hybrid search: Elasticsearch for full-text keyword search and an optional AI Search Service (gRPC) for semantic/LLM-enhanced results. Falls back to Elasticsearch when AI is unavailable or times out.

## Architecture (Clean Architecture, Query-Only)

- **Api/** — gRPC entry point (`SearchGrpcService`), `ExceptionHandlingInterceptor`
- **Application/** — Query handlers (`SearchProductsQuery`), Gateway interfaces, `ApplicationModule.cs`
- **Domain/** — Value objects (`RelevanceScore`, `Money`), entities (`ProductSearchResult`), `IQuery`/`IQueryHandler` interfaces
- **Infrastructure/** — Elasticsearch client + searcher, AI Search gRPC gateway (+ streaming), `NullAiSearchGateway`, `InfrastructureModule.cs`
- **Protos/** — Separate class library (shared by Api and Infrastructure)

## Tech Stack & Conventions

- .NET 8, gRPC with health checks (`Grpc.AspNetCore.HealthChecks`)
- Elasticsearch 8.13 (Elastic.Clients.Elasticsearch)
- AI Search: gRPC client to AiSearchService (optional, feature-flagged)
- Observability: OpenTelemetry
- Testing: NUnit + NSubstitute (unit), xUnit + Testcontainers (integration)
- Module pattern: `AddApplicationServices()`, `AddInfrastructureServices()`

## Search Strategy

1. If `UseAi=true` and AI enabled in config:
   - Try AI Search with 500ms timeout
   - On timeout/error → fallback to Elasticsearch
2. If `UseAi=false` or AI disabled:
   - Direct Elasticsearch query
3. Streaming search (`SearchStream`): progressive results — keyword phase first, then merged

## Code Patterns

- **Gateway pattern**: `IElasticsearchSearcher`, `IAiSearchGateway`, `IAiSearchStreamGateway`
- **NullAiSearchGateway**: Intentionally throws — fallback is exception-driven by design
- **Query handlers**: `IQuery<T>` + `IQueryHandler<TQuery, TResult>` (no MediatR)
- **Pagination**: Page is 1-based, PageSize validated 1-100 in gRPC layer
- **Index initialization**: `ElasticsearchIndexInitializer` runs on startup (`EnsureIndexAsync`)
- **Elasticsearch total hits**: Read from `response.HitsMetadata.Total.Match(...)` — `response.Total` can be 0 even with hits
- **Index existence check**: Uses `exists.ApiCallDetails.HttpStatusCode == 200` (no `Exists` property on `ExistsResponse`)

## Configuration

- Elasticsearch URI
- `AiSearch:Enabled` flag + AI Search gRPC URL
- Health checks for k8s probes
- OpenTelemetry Jaeger endpoint

## Testing

- **Unit**: NUnit + NSubstitute — query handler with mock gateways, AI timeout → ES fallback scenarios
- **Integration**: xUnit + Testcontainers Elasticsearch
- **Contract**: gRPC contract tests

## Key Rules

- Search is read-only — never writes to Elasticsearch (that's Catalog's job)
- AI search timeout is 500ms — keep it fast, fallback is the safety net
- `NullAiSearchGateway` throws by design — don't "fix" it to return empty results
- Proto files are in a shared `Protos` class library — keep in sync with AI Search proto definitions
- Namespaces are `Domain.*`, `Application.*`, `Infrastructure.*`, `Api.*` (no `Search.` prefix internally)

---
applyTo: "AI/AiSearchService/**"
description: "Use when working on the AI Search Service — gRPC hybrid search pipeline combining LLM query parsing, vector search (Qdrant), keyword search (Elasticsearch), and RRF merge."
---

# AI Search Service

## Overview

gRPC service (port 50051) that orchestrates hybrid search: LLM parses the user query, then vector + keyword searches run in parallel, results merge via Reciprocal Rank Fusion (RRF).

## Architecture

- **gRPC** `Search` (unary) and `SearchStream` (bidirectional streaming) — defined in `Protos/ai_search.proto`
- **HTTP** port 8003 — health/ready only, no search endpoints
- Pipeline: `LLM parse → (embed + ES keyword) in parallel → Qdrant vector → RRF merge → paginate`
- LLM has a hard 1.5s timeout; on timeout falls back to raw query with zero confidence

## Tech Stack & Conventions

- Python 3.10+, async throughout
- **Dependencies**: FastAPI (health only), grpcio, httpx, qdrant-client, elasticsearch, pydantic, pydantic-settings, structlog
- **Config**: `pydantic-settings` with `env_prefix="AI_SEARCH_"` in `config.py`
- **Logging**: `structlog` — use `structlog.get_logger()`, log with key-value pairs
- **Models**: dataclasses in `models.py` for internal types, Pydantic BaseModel for API request/response
- **Generated code**: `generated/` folder — do NOT edit `ai_search_pb2.py` / `ai_search_pb2_grpc.py` manually; regenerate from `.proto`

## Code Patterns

- Clients are injected via constructor (no global singletons) — `LLMQueryClient`, `EmbeddingClient`, `QdrantSearchClient`, `ElasticsearchClient`
- `asyncio.create_task` + `asyncio.gather` for parallel I/O
- `asyncio.wait_for` with timeout for LLM calls
- All client methods are async and named simply: `embed()`, `search()`, `parse_query()`
- Streaming RPC cancels previous in-flight search when new query arrives on the same stream

## Testing

- Framework: pytest + pytest-asyncio (asyncio_mode = "auto")
- HTTP mocking: `respx`
- Async mocking: `unittest.mock.AsyncMock`
- Test layout: `tests/unit/`, `tests/integration/`, `tests/e2e/`
- Integration tests use `testcontainers` for Elasticsearch and Qdrant
- Tests are plain `async def test_*` functions — no class-based tests
- Factory helpers like `_make_clients()` return AsyncMock tuples

## Key Rules

- Never block the event loop — all I/O must be awaited
- LLM timeout is non-negotiable (1.5s default) — search must always return even if LLM is slow/down
- RRF k parameter is 60 — don't change without understanding ranking impact
- The service returns product IDs with relevance scores — enrichment (name, price, images) is the caller's responsibility
- Proto files are the contract — changes require coordinating with the Search Service (C#) caller

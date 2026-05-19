---
applyTo: "AI/AiSearchService/**"
description: "Use when working on the AI Search Service — gRPC hybrid search pipeline combining LLM query parsing, vector search (Qdrant), keyword search (Elasticsearch), RRF merge, personalized reranking, and collaborative filtering (frequently bought together)."
---

# AI Search Service

## Overview

gRPC service (port 50051) that orchestrates hybrid search: LLM parses the user query, then vector + keyword searches run in parallel, results merge via Reciprocal Rank Fusion (RRF).

## Architecture

- **gRPC** `Search` (unary), `SearchStream` (bidirectional streaming), `GetSimilarItems` (unary), and `GetFrequentlyBoughtTogether` (unary) — defined in `Protos/ai_search.proto`
- **HTTP** port 8003 — health/ready only, no search endpoints
- Search pipeline: `LLM parse → (embed + ES keyword) in parallel → Qdrant vector → RRF merge → personalized rerank (if user_id) → paginate`
- Personalized reranking: when `user_id` is provided, fetches user preference profile from Redis, boosts results matching category/brand/condition affinities and price comfort zone
- Similar items pipeline: `scroll Qdrant for source vector → vector search with exclusion filter → return ranked results` (no ES, no LLM)
- Frequently bought together pipeline: `Redis ZREVRANGE on cooccurrence:purchase:{catalog_item_id} → return ranked co-occurrence items` (no ES, no LLM, no Qdrant)
- LLM has a hard 1.5s timeout; on timeout falls back to raw query with zero confidence

## Tech Stack & Conventions

- Python 3.10+, async throughout
- **Dependencies**: FastAPI (health only), grpcio, httpx, qdrant-client, elasticsearch, redis.asyncio, pydantic, pydantic-settings, structlog
- **Config**: `pydantic-settings` with `env_prefix="AI_SEARCH_"` in `config.py`
- **Logging**: `structlog` — use `structlog.get_logger()`, log with key-value pairs
- **Models**: dataclasses in `models.py` for internal types, Pydantic BaseModel for API request/response
- **Generated code**: `generated/` folder — do NOT edit `ai_search_pb2.py` / `ai_search_pb2_grpc.py` manually; regenerate from `.proto`

## Code Patterns

- Clients are injected via constructor (no global singletons) — `LLMQueryClient`, `EmbeddingClient`, `QdrantSearchClient`, `ElasticsearchClient`
- `asyncio.create_task` + `asyncio.gather` for parallel I/O
- `asyncio.wait_for` with timeout for LLM calls
- All client methods are async and named simply: `embed()`, `search()`, `parse_query()`, `find_similar()`
- Streaming RPC cancels previous in-flight search when new query arrives on the same stream
- `QdrantSearchClient.find_similar()` uses scroll-by-payload to get source vector, then vector search with `HasIdCondition` must_not to exclude the source point
- `reranker.py` — `rerank()` function fetches user preference profile from Redis, retrieves product payloads from Qdrant, computes affinity boosts (category, brand, condition, price range), and reorders results; returns items unchanged on any failure
- `GetFrequentlyBoughtTogether` handler in `grpc_server.py` reads directly from Redis sorted set `cooccurrence:purchase:{catalog_item_id}` — no orchestrator needed

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

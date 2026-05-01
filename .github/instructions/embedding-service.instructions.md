---
applyTo: "AI/EmbeddingService/**"
description: "Use when working on the Embedding Service — thin bridge between Ollama and the rest of the system, exposing embeddings via REST and gRPC streaming."
---

# Embedding Service

## Overview

Centralizes embedding generation so other services don't need to know about Ollama. Exposes two interfaces:

- **REST** `POST /embed` (port 8001) — used by AiSearchService for one-off embedding during search
- **gRPC streaming** `EmbedStream` (port 50052) — used by VectorIndexerWorker for long-lived bidirectional streaming

## Architecture

- Ollama client sends `POST /api/embeddings` with model + prompt, returns a float vector
- Default model: `nomic-embed-text` (768 dimensions) — can be overridden per request
- 30s timeout to Ollama, no retries — if Ollama is down you get a 5xx
- gRPC keepalive configured for long idle periods (VectorIndexerWorker may go quiet between Kafka messages)

## Tech Stack & Conventions

- Python 3.10+, async throughout
- **Dependencies**: FastAPI, grpcio, httpx, pydantic, pydantic-settings, structlog, uvicorn
- **Config**: `pydantic-settings` with `env_prefix="EMBEDDING_"` in `config.py`
- **Logging**: `structlog` — use `structlog.get_logger()`, log with key-value pairs
- **Models**: Pydantic `BaseModel` in `models.py` — `EmbedRequest`, `EmbedResponse`
- **Generated code**: `embedding_pb2.py` / `embedding_pb2_grpc.py` — do NOT edit manually; regenerate from `protos/embedding.proto`

## Code Patterns

- `OllamaClient` in `clients/ollama_client.py` — thin httpx wrapper, one method: `embed(text, model) → list[float]`
- FastAPI route uses `Depends()` for `OllamaClient` injection (overridden in app startup)
- gRPC servicer in `grpc_server.py` — `EmbedStream` is an async generator iterating over request_iterator
- `correlation_id` on gRPC messages matches request to response (future-proofing for concurrent sends)
- `build_grpc_server()` factory creates and configures the gRPC server instance

## Testing

- Framework: pytest + pytest-asyncio (asyncio_mode = "auto")
- HTTP mocking: `respx` (mock Ollama responses)
- Test layout: `tests/unit/`, `tests/e2e/`
- Unit tests mock the httpx layer, not Ollama itself
- Tests are plain `async def test_*` functions — no class-based tests

## Key Rules

- Never add retries to Ollama calls — the caller should handle failures
- Model name must always be forwarded to Ollama (don't hardcode in client)
- The gRPC stream must handle server-side errors gracefully via `context.abort()`
- Keep the REST and gRPC paths functionally equivalent — same Ollama call underneath
- `/health` and `/ready` endpoints exist for k8s probes — keep them simple

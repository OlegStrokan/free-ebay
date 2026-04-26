# Embedding Service

- Bridge between Ollama and the rest of the system
- Centralizes model config so other services don't need to know about Ollama
- 30s timeout to Ollama. No retries. If Ollama is down, you get a 5xx, deal with it

## Two ways to call it

### REST (HTTP)

```
POST /embed
{ "texts": ["..."], "model": "nomic-embed-text" }  // model is optional
→ { "embeddings": [[...]], "model": "...", "dimensions": 768 }
```
Model defaults to `nomic-embed-text`. Can be overridden per request.

### gRPC streaming (port 50052)
Used by VectorIndexerWorker. One stream stays open for the worker's lifetime instead of a new HTTP call per product event. Worker sends a text, gets a vector back, sends the next text, and so on - all over the same connection. Saves per-call HTTP overhead and proto is faster to serialize than JSON.

```
EmbedStream(stream EmbedStreamRequest) returns (stream EmbedStreamResponse)

EmbedStreamRequest  { correlation_id, text, model }
EmbedStreamResponse { correlation_id, vector, dimensions }
```

correlation_id is just so the caller can match response to request — the stream is sequential so it's not strictly needed right now, but it's there for when concurrent sends are added.

essentially it takes this:

```
Logitech MX Keys | Wireless keyboard with backlit keys | Keyboards | color: graphite | brand: logitech | layout: full-size
```
and returns this:

```
[0.021, -0.143, 0.887, 0.034, -0.512, ...]  // 768 numbers for our model (different model have different dimension value)
```

## Config (env prefix `EMBEDDING_`)

| var | default |
|---|---|
| `OLLAMA_BASE_URL` | `http://localhost:11434` |
| `DEFAULT_MODEL` | `nomic-embed-text` |
| `PORT` | `8001` |
| `GRPC_PORT` | `50052` |

## Also has `/health` and `/ready`

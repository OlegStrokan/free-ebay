# AI Search Service

Orchestrates the full hybrid search pipeline.

```
query → LLM parse (1.5s timeout)
              ↓
       embed + ES search  ← run in parallel
              ↓
       qdrant search (needs the vector)
              ↓
       RRF merge (k=60) → paginated results
```

LLM timeout is hard at 1.5s. If it times out, falls back to raw query, no filters. Search still works, just dumber.

- **gRPC** port 50051 — `Search(SearchRequest) → SearchResponse` — this is the actual search
- **HTTP** port 8003 — health/ready only, no search endpoint over HTTP

Needs all of these running: EmbeddingService, LLMQueryService, Qdrant, Elasticsearch.


we also have cool "similar products" feature which essentially is:

1. fetch the source vector
2. build filters, some mathing type shit
3. exclude self from similar list
4. run similarity search
5. map and return
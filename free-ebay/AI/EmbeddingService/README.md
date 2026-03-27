# Embedding Service

- Bridge between Ollama and the rest of the system
- Centralizes model config so other services don't need to know about Ollama
- 30s timeout to Ollama. No retries. If Ollama is down, you get a 5xx, deal with it

## Endpoint

```
POST /embed
{ "texts": ["..."], "model": "nomic-embed-text" }  // model is optional
→ { "embeddings": [[...]], "model": "...", "dimensions": 768 }
```

Model defaults to `nomic-embed-text`. Can be overridden per request.

## Config (env prefix `EMBEDDING_`)

| var | default |
|---|---|
| `OLLAMA_BASE_URL` | `http://localhost:11434` |
| `DEFAULT_MODEL` | `nomic-embed-text` |
| `PORT` | `8001` |

## Also has `/health` and `/ready`


Current service is only one which can completely moved from parents - it's just ollama http wrapper with stable api
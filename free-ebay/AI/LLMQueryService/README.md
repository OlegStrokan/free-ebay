# LLM Query Service

Converts raw NL query into structured filters + keywords.

```
"red keyboard under 50 bucks without numpad"
→ { semantic_query, filters: { price_max, color, ... }, keywords: [...], confidence: 0.87 }
```

## Endpoint

```
POST /parse-query
{ "query": "..." }
→ ParseQueryResponse
```

If the LLM times out or returns garbage — fallback kicks in: `semantic_query = raw_query`, `confidence = 0.0`, keywords = query.split(). Caller always gets a response.

## Config (env prefix `LLM_`)

Low temperature (in config.py) on purpose - we want deterministic JSON, not creative writing.
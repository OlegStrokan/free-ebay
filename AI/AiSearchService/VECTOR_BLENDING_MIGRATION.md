# Task 4B: Vector Blending Migration Plan

## What This Is

Migration guide for replacing the current post-RRF score boosting reranker (Task 4A) with pre-retrieval vector blending (Task 4B) in the search pipeline.

## Current State (4A — Score Boosting)

```
query → embed → Qdrant search → ES search → RRF merge → rerank_with_preferences() → results
                                                                    ↑
                                              reads user profile from Redis
                                              retrieves product payloads from Qdrant
                                              applies affinity multipliers (+30% category, +20% brand, etc.)
                                              reorders results
```

**Files involved:**
- `pipeline/reranker.py` — reranking logic
- `pipeline/orchestrator.py` — calls `rerank_with_preferences()` after RRF merge
- `pipeline/streaming_orchestrator.py` — same for streaming path

## Target State (4B — Vector Blending)

```
query → embed → blend(query_vec, user_pref_vec, α) → Qdrant search with blended vector → ES search → RRF merge → results
                         ↑
          reads user preference VECTOR from Redis
          formula: final_vec = α * query_vec + (1-α) * pref_vec
          α = 0.85 (suggested starting point)
```

## Migration Steps

### 1. Build preference vectors (UserPreferenceWorker)

The worker currently stores a JSON profile. Add a step that also computes and stores a dense embedding vector per user.

**Where:** `AI/UserPreferenceWorker/aggregator.py`

**Logic:**
- On each profile recomputation, take the user's top-N interacted item IDs (by decayed weight)
- Fetch their embeddings from Qdrant (gRPC call to EmbeddingService or direct Qdrant scroll)
- Compute weighted average: `pref_vec = Σ(weight_i * vec_i) / Σ(weight_i)`
- Store in Redis: `user:{user_id}:preference_vector` as binary blob (numpy `.tobytes()`)
- TTL: same as profile (30 days)

**New dependency:** UserPreferenceWorker needs a Qdrant client or EmbeddingService gRPC client.

### 2. Modify orchestrator to blend before Qdrant search

**Where:** `pipeline/orchestrator.py`, `pipeline/streaming_orchestrator.py`

**Change:**
```python
# Before (current):
query_embedding = await embedding_client.embed(parsed_query.text)
qdrant_results = await qdrant.search(query_embedding, top_k)

# After (4B):
query_embedding = await embedding_client.embed(parsed_query.text)
if user_id and redis:
    pref_vec_bytes = await redis.get(f"user:{user_id}:preference_vector")
    if pref_vec_bytes:
        pref_vec = np.frombuffer(pref_vec_bytes, dtype=np.float32)
        alpha = settings.vector_blend_alpha  # 0.85
        query_embedding = alpha * query_embedding + (1 - alpha) * pref_vec
qdrant_results = await qdrant.search(query_embedding, top_k)
```

### 3. Remove reranker (optional)

**If replacing 4A entirely:**
- Delete the `rerank_with_preferences()` call from both orchestrators
- Delete `pipeline/reranker.py`
- Remove related tests

**If keeping both (recommended during transition):**
- Keep 4A as a safety net with reduced weights (e.g., halve all multipliers)
- Add a feature flag `AI_SEARCH_USE_VECTOR_BLENDING=true/false`

### 4. Add config

```env
AI_SEARCH_VECTOR_BLEND_ALPHA=0.85
AI_SEARCH_USE_VECTOR_BLENDING=false  # flip to true when ready
```

### 5. Update tests

- New unit tests for vector blending math (alpha=0 gives pure pref, alpha=1 gives pure query)
- Test fallback: missing preference vector → use raw query embedding unchanged
- Test numpy dtype handling (float32 round-trip through Redis)

## Why We Skipped 4B For Now

1. **No preference vectors exist yet.** The UserPreferenceWorker stores JSON profiles (categories, brands, prices), not dense embedding vectors. Building vectors requires either:
   - A Qdrant client in the worker (new dependency, new failure mode)
   - A batch job that periodically recomputes vectors from interaction history
   
2. **Cold start problem is worse.** 4A works with even 1 interaction (if user viewed a Sony camera, Sony products get boosted). 4B needs enough interactions to build a *meaningful average vector* — with 1-2 items, the preference vector is just noise.

3. **Tuning α requires A/B testing infrastructure.** Wrong α values either:
   - Too high (α > 0.95): negligible personalization
   - Too low (α < 0.7): filter bubble, results drift from query intent
   
   We don't have A/B testing infra to measure the impact.

4. **4A is already live and working.** It provides measurable personalization with zero risk of degrading relevance. Ship and iterate.

## When To Implement 4B

Implement when ALL of these are true:

- [ ] You have >1000 users with >10 interactions each (enough for stable preference vectors)
- [ ] You have A/B testing infrastructure to measure search relevance impact
- [ ] You observe that users are missing relevant results (4A can't surface items not in top-K)
- [ ] You've validated with offline evaluation that blended queries produce better recall@50

## Rollout Strategy

1. Build preference vectors in UserPreferenceWorker (background, no user impact)
2. Add vector blending behind feature flag (default off)
3. A/B test: 50/50 split — 4A-only vs 4B+4A
4. If 4B wins on click-through and purchase rate, disable 4A reranker
5. Tune α based on metrics (start at 0.85, sweep 0.7–0.95)

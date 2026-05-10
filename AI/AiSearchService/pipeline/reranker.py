from __future__ import annotations

import json
import structlog
from typing import TYPE_CHECKING

from models import ScoredResult

if TYPE_CHECKING:
    from qdrant_client import AsyncQdrantClient
    from redis.asyncio import Redis

log = structlog.get_logger()

# Affinity boost weights - keep modest to avoid overriding relevance
_CATEGORY_MAX_BOOST = 0.3
_BRAND_MAX_BOOST = 0.2
_PRICE_BOOST = 0.1
_CONDITION_MAX_BOOST = 0.1


async def rerank_with_preferences(
    results: list[ScoredResult],
    user_id: str,
    redis: Redis,
    qdrant: AsyncQdrantClient,
    collection: str,
) -> list[ScoredResult]:
    """Apply user preference affinity multipliers to RRF scores.

    Returns the same list re-sorted by boosted scores. If the user has no
    preference profile, returns results unchanged.
    """
    if not results or not user_id:
        return results

    profile_json = await redis.get(f"user:{user_id}:preference_profile")
    if not profile_json:
        log.debug("no_preference_profile", user_id=user_id)
        return results

    try:
        profile = json.loads(profile_json)
    except (json.JSONDecodeError, TypeError):
        log.warning("invalid_preference_profile", user_id=user_id)
        return results

    # Fetch product metadata from Qdrant for the merged result IDs
    point_ids = [r.product_id for r in results]
    try:
        points = await qdrant.retrieve(
            collection_name=collection,
            ids=point_ids,
            with_payload=True,
            with_vectors=False,
        )
    except Exception:
        log.warning("qdrant_retrieve_failed_for_reranking", count=len(point_ids))
        return results

    payload_map: dict[str, dict] = {}
    for p in points:
        pid = p.payload.get("product_id", str(p.id))
        payload_map[pid] = p.payload

    # Normalize profile weights to [0, 1] range
    categories = profile.get("categories", {})
    brands = profile.get("brands", {})
    condition_weights = profile.get("condition_weights", {})
    price_p25 = profile.get("price_p25")
    price_p75 = profile.get("price_p75")

    max_cat = max(categories.values(), default=1.0)
    max_brand = max(brands.values(), default=1.0)
    max_cond = max(condition_weights.values(), default=1.0)

    reranked = []
    for r in results:
        multiplier = 1.0
        payload = payload_map.get(r.product_id)
        if not payload:
            reranked.append(r)
            continue

        # Category affinity
        cat = payload.get("category", "")
        if cat and cat in categories:
            norm = categories[cat] / max_cat
            multiplier += _CATEGORY_MAX_BOOST * norm

        # Brand affinity
        brand = payload.get("brand", "")
        if brand and brand in brands:
            norm = brands[brand] / max_brand
            multiplier += _BRAND_MAX_BOOST * norm

        # Price range comfort
        price = payload.get("min_price")
        if price is not None and price_p25 is not None and price_p75 is not None:
            if price_p25 <= price <= price_p75:
                multiplier += _PRICE_BOOST

        # Condition preference
        condition = payload.get("best_condition", "")
        if condition and condition in condition_weights:
            norm = condition_weights[condition] / max_cond
            multiplier += _CONDITION_MAX_BOOST * norm

        reranked.append(ScoredResult(product_id=r.product_id, score=r.score * multiplier))

    reranked.sort(key=lambda x: x.score, reverse=True)
    log.info("reranking_applied", user_id=user_id, items=len(reranked))
    return reranked

import structlog
import redis.asyncio as redis

from config import settings

log = structlog.get_logger()

_PURCHASE_HISTORY_KEY = "user:{user_id}:recent_purchases"
_COOCCURRENCE_KEY = "cooccurrence:purchase:{item_id}"

# How many recent purchases per user to keep for co-occurrence pairing
_MAX_RECENT_PURCHASES = 50
# How long recent purchase lists live (90 days)
_RECENT_PURCHASES_TTL = 90 * 86400
# Co-occurrence sorted set TTL (180 days of inactivity)
_COOCCURRENCE_TTL = 180 * 86400


class CoOccurrenceTracker:
    def __init__(self, redis_client: redis.Redis) -> None:
        self._redis = redis_client

    async def record_purchase(self, user_id: str, catalog_item_id: str) -> None:
        """Record a purchase and update co-occurrence counts with recent purchases."""
        history_key = _PURCHASE_HISTORY_KEY.format(user_id=user_id)

        # Get recent purchases by this user (before adding current)
        recent_items: list[str] = await self._redis.lrange(history_key, 0, _MAX_RECENT_PURCHASES - 1)

        if recent_items:
            # Create pairwise co-occurrence: new item <> each recent item
            pipe = self._redis.pipeline()
            for other_item in recent_items:
                if other_item == catalog_item_id:
                    continue
                # Bidirectional: A->B and B->A
                key_a = _COOCCURRENCE_KEY.format(item_id=catalog_item_id)
                key_b = _COOCCURRENCE_KEY.format(item_id=other_item)
                pipe.zincrby(key_a, 1, other_item)
                pipe.zincrby(key_b, 1, catalog_item_id)
                pipe.expire(key_a, _COOCCURRENCE_TTL)
                pipe.expire(key_b, _COOCCURRENCE_TTL)
            await pipe.execute()

            log.debug(
                "cooccurrence_updated",
                user_id=user_id,
                item=catalog_item_id,
                paired_with=len(recent_items),
            )

        # Add current purchase to history
        await self._redis.lpush(history_key, catalog_item_id)
        await self._redis.ltrim(history_key, 0, _MAX_RECENT_PURCHASES - 1)
        await self._redis.expire(history_key, _RECENT_PURCHASES_TTL)

    async def get_co_occurrences(
        self, catalog_item_id: str, limit: int = 10
    ) -> list[tuple[str, float]]:
        """Return top co-occurring items sorted by count descending."""
        key = _COOCCURRENCE_KEY.format(item_id=catalog_item_id)
        # ZREVRANGE with scores: highest counts first
        results = await self._redis.zrevrange(key, 0, limit - 1, withscores=True)
        return [(member, score) for member, score in results]

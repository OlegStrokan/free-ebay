import asyncio
import structlog
import redis.asyncio as redis
from aggregator import PreferenceAggregator
from consumer import run_consumer
from config import settings

log = structlog.get_logger()


async def main() -> None:
    redis_client = redis.from_url(settings.redis_url, decode_responses=True)

    aggregator = PreferenceAggregator(redis_client=redis_client)

    log.info("user_preference_worker_starting")
    try:
        await run_consumer(aggregator)
    finally:
        await redis_client.aclose()


if __name__ == "__main__":
    asyncio.run(main())

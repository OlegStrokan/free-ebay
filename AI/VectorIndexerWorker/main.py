import asyncio
import structlog
from embedding_client import EmbeddingClient
from qdrant_client import QdrantIndexClient
from indexer import Indexer
from consumer import run_consumer

log = structlog.get_logger()

async def main() -> None:
    embedding = EmbeddingClient()
    qdrant = QdrantIndexClient()
    await qdrant.ensure_collection()

    indexer = Indexer(embedding=embedding, qdrant=qdrant)
    log.info("vector_indexer_worker_starting")
    try:
        await run_consumer(indexer)
    finally:
        await embedding.aclose()

if __name__ == "__main__":
    asyncio.run(main())
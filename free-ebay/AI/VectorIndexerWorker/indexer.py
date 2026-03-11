import structlog
from embedding_client import EmbeddingClient
from models import ProductEvent
from qdrant_client import QdrantIndexClient

log = structlog.get_logger()

def build_product_corpus(event: ProductEvent) -> str:
    parts = [
        event.name,
        event.description,
        event.category
    ]
    for attr in event.attributes:
        parts.append(f"{attr.key}: {attr.value}")
    return " | ".join(filter(None, parts))

class Indexer:
    def __init__(self, embedding: EmbeddingClient, qdrant: QdrantIndexClient) -> None:
        self.embedding = embedding
        self.qdrant = qdrant

    async def upsert(self, raw: dict) -> None:
        event = ProductEvent.model_validate(raw)
        corpus = build_product_corpus(event)
        vectors = await self.embedding.embed_batch([corpus])
        vector = vectors[0]

        payload = {
            "product_id": event.product_id,
            "name": event.name,
            "category": event.category,
            "price": float(event.price),
            "currency": event.currency,
            "color": next((a.value for a in event.attributes if a.key == "color"), None),
            "layout": next((a.value for a in event.attributes if a.key == "layout"), None),
            "brand": next((a.value for a in event.attributes if a .key == "brand"), None),
            "stocks": event.stock_quality,
            "status": "active" if event.stock_quality > 0 else "out_of_stock",
            "image_urls": event.image_urls,
        }

        await self.qdrant.upsert(event.product_id, vector, payload)
        log.info("product_indexed", product_id=event.product_id)

    async def delete(self, product_id: str) -> None:
        await self.qdrant.delete(product_id)
        log.info("product_deleted_from_index", product_id=product_id)
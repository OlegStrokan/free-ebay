import structlog
from models import ProductEvent, ProductStockUpdatedEvent, CatalogItemEvent, CatalogItemListingSummaryEvent

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

def build_catalog_item_corpus(event: CatalogItemEvent) -> str:
    parts = [
        event.Name,
        event.Description,
    ]
    for attr in event.Attributes:
        parts.append(f"{attr.key}: {attr.value}")
    return " | ".join(filter(None, parts))

class Indexer:
    def __init__(self, embedding, qdrant) -> None:
        self._embedding = embedding
        self.qdrant = qdrant

    async def upsert(self, raw: dict) -> None:
        event = ProductEvent.model_validate(raw)
        corpus = build_product_corpus(event)
        vector = await self._embedding.embed(corpus)

        payload = {
            "product_id": event.product_id,
            "product_type": "product",
            "name": event.name,
            "category": event.category,
            "price": float(event.price),
            "currency": event.currency,
            "color": next((a.value for a in event.attributes if a.key == "color"), None),
            "layout": next((a.value for a in event.attributes if a.key == "layout"), None),
            "brand": next((a.value for a in event.attributes if a .key == "brand"), None),
            "stock_quantity": event.stock_quantity,
            "status": "active" if event.stock_quantity > 0 else "out_of_stock",
            "image_urls": event.image_urls,
        }

        await self.qdrant.upsert(event.product_id, vector, payload)
        log.info("product_indexed", product_id=event.product_id)

    async def delete(self, product_id: str) -> None:
        await self.qdrant.delete(product_id)
        log.info("product_deleted_from_index", product_id=product_id)

    async def update_stock(self, raw: dict) -> None:
        event = ProductStockUpdatedEvent.model_validate(raw)
        status = "active" if event.new_quantity > 0 else "out_of_stock"
        await self.qdrant.update_payload(
            product_id=event.product_id,
            patch={"stock_quantity": event.new_quantity, "status": status},
        )
        log.info("product_stock_updated", product_id=event.product_id, status=status)

    async def upsert_catalog_item(self, raw: dict) -> None:
        event = CatalogItemEvent.model_validate(raw)
        catalog_item_id = event.CatalogItemId.Value
        corpus = build_catalog_item_corpus(event)
        vector = await self._embedding.embed(corpus)

        payload = {
            "product_id": catalog_item_id,
            "product_type": "catalog_item",
            "name": event.Name,
            "category": event.CategoryId.Value,
            "color": next((a.value for a in event.Attributes if a.key == "color"), None),
            "layout": next((a.value for a in event.Attributes if a.key == "layout"), None),
            "brand": next((a.value for a in event.Attributes if a.key == "brand"), None),
            "image_urls": event.ImageUrls,
            # Listing-aggregate fields - updated by update_listing_summary when listings change
            "min_price": None,
            "min_price_currency": None,
            "seller_count": 0,
            "has_active_listings": False,
            "best_condition": None,
            "total_stock": 0,
            "status": "out_of_stock",
        }

        await self.qdrant.upsert(catalog_item_id, vector, payload)
        log.info("catalog_item_indexed", catalog_item_id=catalog_item_id)

    async def update_listing_summary(self, raw: dict) -> None:
        event = CatalogItemListingSummaryEvent.model_validate(raw)
        catalog_item_id = event.CatalogItemId.Value

        patch = {
            "min_price": event.MinPrice,
            "min_price_currency": event.MinPriceCurrency,
            "seller_count": event.SellerCount,
            "has_active_listings": event.HasActiveListings,
            "best_condition": event.BestCondition,
            "total_stock": event.TotalStock,
            "status": "active" if event.HasActiveListings else "out_of_stock",
        }

        await self.qdrant.update_payload(product_id=catalog_item_id, patch=patch)
        log.info(
            "listing_summary_updated",
            catalog_item_id=catalog_item_id,
            seller_count=event.SellerCount,
            min_price=event.MinPrice,
            has_active=event.HasActiveListings,
        )
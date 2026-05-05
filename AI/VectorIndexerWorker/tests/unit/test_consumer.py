import json
from unittest.mock import AsyncMock, MagicMock

import pytest

from consumer import process_event
from indexer import Indexer


def _make_indexer() -> Indexer:
    embedding = AsyncMock()
    embedding.embed.return_value = [0.1] * 4
    qdrant = AsyncMock()
    return Indexer(embedding, qdrant)


def _make_msg(event_type: str, payload: dict, extra_headers: list | None = None) -> MagicMock:
    headers = [(b"EventType", event_type.encode())]
    if extra_headers:
        headers.extend(extra_headers)
    msg = MagicMock()
    msg.headers.return_value = headers
    msg.value.return_value = json.dumps(payload).encode()
    return msg


_PRODUCT_PAYLOAD = {
    "product_id": "prod-1",
    "name": "Keyboard",
    "description": "tactile",
    "category": "keyboards",
    "price": 80.0,
    "currency": "USD",
    "stock_quantity": 5,
    "image_urls": [],
    "attributes": [],
}


async def test_create_event_calls_upsert(  ) -> None:
    indexer = _make_indexer()
    msg = _make_msg("ProductCreateEvent", _PRODUCT_PAYLOAD)
    await process_event(msg, indexer)
    indexer.qdrant.upsert.assert_called_once()


async def test_updated_event_calls_upsert() -> None:
    indexer = _make_indexer()
    msg = _make_msg("ProductUpdatedEvent", _PRODUCT_PAYLOAD)
    await process_event(msg, indexer)
    indexer.qdrant.upsert.assert_called_once()


async def test_deleted_event_calls_delete() -> None:
    indexer = _make_indexer()
    msg = _make_msg("ProductDeletedEvent", {"product_id": "prod-1"})
    await process_event(msg, indexer)
    indexer.qdrant.delete.assert_called_once_with("prod-1")


async def test_unknown_event_type_does_not_raise_or_call_indexer() -> None:
    indexer = _make_indexer()
    msg = _make_msg("UnknownEvent", _PRODUCT_PAYLOAD)
    # Must not raise
    await process_event(msg, indexer)
    indexer.qdrant.upsert.assert_not_called()
    indexer.qdrant.delete.assert_not_called()


_CATALOG_ITEM_PAYLOAD = {
    "CatalogItemId": {"Value": "ci-1"},
    "Name": "iPhone 16 Pro Max",
    "Description": "Apple flagship",
    "CategoryId": {"Value": "smartphones"},
    "Gtin": None,
    "Attributes": [],
    "ImageUrls": [],
}

_LISTING_SUMMARY_PAYLOAD = {
    "CatalogItemId": {"Value": "ci-1"},
    "MinPrice": 899.99,
    "MinPriceCurrency": "USD",
    "SellerCount": 3,
    "HasActiveListings": True,
    "BestCondition": "New",
    "TotalStock": 47,
}


async def test_catalog_item_created_event_calls_upsert_catalog_item() -> None:
    indexer = _make_indexer()
    msg = _make_msg("CatalogItemCreatedEvent", _CATALOG_ITEM_PAYLOAD)
    await process_event(msg, indexer)
    indexer.qdrant.upsert.assert_called_once()


async def test_catalog_item_updated_event_calls_upsert_catalog_item() -> None:
    indexer = _make_indexer()
    msg = _make_msg("CatalogItemUpdatedEvent", _CATALOG_ITEM_PAYLOAD)
    await process_event(msg, indexer)
    indexer.qdrant.upsert.assert_called_once()


async def test_listing_summary_updated_event_calls_update_payload() -> None:
    indexer = _make_indexer()
    msg = _make_msg("CatalogItemListingSummaryUpdatedEvent", _LISTING_SUMMARY_PAYLOAD)
    await process_event(msg, indexer)
    indexer.qdrant.update_payload.assert_called_once()

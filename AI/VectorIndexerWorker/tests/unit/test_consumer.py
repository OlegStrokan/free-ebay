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

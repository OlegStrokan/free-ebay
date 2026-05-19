import json
import pytest
from unittest.mock import AsyncMock, MagicMock
from confluent_kafka import KafkaError

from consumer import process_event
from models import EventType


@pytest.fixture
def aggregator() -> AsyncMock:
    mock = AsyncMock()
    return mock


@pytest.fixture
def cooccurrence() -> AsyncMock:
    mock = AsyncMock()
    return mock


def _make_message(event_type: str, payload: dict) -> MagicMock:
    msg = MagicMock()
    msg.headers.return_value = [("event-type", event_type.encode())]
    msg.value.return_value = json.dumps(payload).encode()
    return msg


async def test_process_product_viewed_event(aggregator):
    payload = {
        "user_id": "user-1",
        "catalog_item_id": "item-1",
        "duration_ms": 5000,
        "source": "search",
        "category": "Electronics",
        "brand": "Sony",
        "price": 299.99,
        "condition": "New",
    }
    msg = _make_message(EventType.PRODUCT_VIEWED, payload)

    await process_event(msg, aggregator)

    aggregator.record_view.assert_called_once()
    call_args = aggregator.record_view.call_args[0][0]
    assert call_args.user_id == "user-1"
    assert call_args.catalog_item_id == "item-1"
    assert call_args.brand == "Sony"


async def test_process_product_clicked_event(aggregator):
    payload = {
        "user_id": "user-2",
        "catalog_item_id": "item-2",
        "query_text": "headphones",
        "rank": 3,
    }
    msg = _make_message(EventType.PRODUCT_CLICKED, payload)

    await process_event(msg, aggregator)

    aggregator.record_click.assert_called_once()
    call_args = aggregator.record_click.call_args[0][0]
    assert call_args.user_id == "user-2"
    assert call_args.query_text == "headphones"


async def test_process_purchase_completed_event(aggregator, cooccurrence):
    payload = {
        "user_id": "user-3",
        "catalog_item_id": "item-3",
        "listing_id": "listing-1",
        "price": 150.0,
        "category": "Audio",
        "brand": "Bose",
        "condition": "New",
    }
    msg = _make_message(EventType.PURCHASE_COMPLETED, payload)

    await process_event(msg, aggregator, cooccurrence)

    aggregator.record_purchase.assert_called_once()
    call_args = aggregator.record_purchase.call_args[0][0]
    assert call_args.user_id == "user-3"
    assert call_args.listing_id == "listing-1"

    cooccurrence.record_purchase.assert_called_once_with("user-3", "item-3")


async def test_process_search_bounced_event(aggregator):
    payload = {
        "user_id": "user-4",
        "query_text": "nonexistent product",
    }
    msg = _make_message(EventType.SEARCH_BOUNCED, payload)

    await process_event(msg, aggregator)

    # search bounce is logged but no aggregator method called yet
    aggregator.record_view.assert_not_called()
    aggregator.record_click.assert_not_called()
    aggregator.record_purchase.assert_not_called()


async def test_process_unknown_event_type(aggregator):
    payload = {"some": "data"}
    msg = _make_message("UnknownEvent", payload)

    # should not raise
    await process_event(msg, aggregator)


async def test_purchase_without_cooccurrence_still_records_purchase(aggregator):
    """When cooccurrence is None, purchase event still records via aggregator without crashing."""
    payload = {
        "user_id": "user-5",
        "catalog_item_id": "item-5",
        "listing_id": "listing-5",
        "price": 200.0,
        "category": "Books",
        "brand": "Penguin",
        "condition": "New",
    }
    msg = _make_message(EventType.PURCHASE_COMPLETED, payload)

    await process_event(msg, aggregator, cooccurrence=None)

    aggregator.record_purchase.assert_called_once()
    call_args = aggregator.record_purchase.call_args[0][0]
    assert call_args.user_id == "user-5"
    assert call_args.catalog_item_id == "item-5"

    # cooccurrence was None, so no crash and no co-occurrence call needed
    aggregator.record_view.assert_not_called()
    aggregator.record_click.assert_not_called()

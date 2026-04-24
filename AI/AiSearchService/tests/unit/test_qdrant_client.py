from unittest.mock import AsyncMock, MagicMock

import pytest

from clients.qdrant_client import QdrantSearchClient
from models import Filters


@pytest.fixture
def client() -> QdrantSearchClient:
    c = QdrantSearchClient(url="http://qdrant-test:6333", collection="products")
    c._client = AsyncMock()
    c._client.search.return_value = []
    return c


async def test_search_always_includes_active_status_filter(client: QdrantSearchClient) -> None:
    await client.search([0.1, 0.2], Filters(), top_k=10)

    call_kwargs = client._client.search.call_args.kwargs
    must_conditions = call_kwargs["query_filter"].must
    keys = [c.key for c in must_conditions]
    assert "status" in keys


async def test_search_appends_price_max_range_filter(client: QdrantSearchClient) -> None:
    await client.search([0.1], Filters(price_max=100.0), top_k=10)

    call_kwargs = client._client.search.call_args.kwargs
    conditions = call_kwargs["query_filter"].must
    price_conditions = [c for c in conditions if c.key == "price"]
    assert any(c.range.lte == 100.0 for c in price_conditions)


async def test_search_appends_price_min_range_filter(client: QdrantSearchClient) -> None:
    await client.search([0.1], Filters(price_min=20.0), top_k=10)

    call_kwargs = client._client.search.call_args.kwargs
    conditions = call_kwargs["query_filter"].must
    price_conditions = [c for c in conditions if c.key == "price"]
    assert any(c.range.gte == 20.0 for c in price_conditions)


async def test_search_appends_color_match_filter(client: QdrantSearchClient) -> None:
    await client.search([0.1], Filters(color="Red"), top_k=10)

    call_kwargs = client._client.search.call_args.kwargs
    conditions = call_kwargs["query_filter"].must
    color_conditions = [c for c in conditions if c.key == "color"]
    assert len(color_conditions) == 1
    assert color_conditions[0].match.value == "red"


async def test_numpad_exclusion_adds_layout_filter(client: QdrantSearchClient) -> None:
    await client.search([0.1], Filters(attributes_excluded=["numpad"]), top_k=10)

    call_kwargs = client._client.search.call_args.kwargs
    conditions = call_kwargs["query_filter"].must
    layout_conditions = [c for c in conditions if c.key == "layout"]
    assert len(layout_conditions) == 1

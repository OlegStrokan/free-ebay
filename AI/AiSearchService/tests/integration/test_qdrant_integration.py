import pytest
from qdrant_client.models import PointStruct

from clients.qdrant_client import QdrantSearchClient
from models import Filters
from tests.integration.conftest import (
    VECTOR_SIZE,
    drop_qdrant_collection,
    seed_qdrant_collection,
)

COLLECTION = "test_products_qdrant"

_POINTS = [
    PointStruct(
        id="aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        vector=[1.0, 0.0, 0.0, 0.0],
        payload={"status": "active", "price": 80.0, "color": "black", "name": "Keyboard A"},
    ),
    PointStruct(
        id="bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        vector=[0.0, 1.0, 0.0, 0.0],
        payload={"status": "active", "price": 200.0, "color": "white", "name": "Monitor B"},
    ),
    PointStruct(
        id="cccccccc-cccc-cccc-cccc-cccccccccccc",
        vector=[0.9, 0.1, 0.0, 0.0],
        payload={"status": "out_of_stock", "price": 50.0, "color": "black", "name": "Keyboard C"},
    ),
]


@pytest.fixture(autouse=True)
async def qdrant_collection(qdrant_url: str):
    await seed_qdrant_collection(qdrant_url, COLLECTION, _POINTS)
    yield
    await drop_qdrant_collection(qdrant_url, COLLECTION)


async def test_vector_search_returns_nearest_by_cosine(qdrant_url: str) -> None:
    client = QdrantSearchClient(url=qdrant_url, collection=COLLECTION)
    # Query vector close to point A
    results = await client.search(vector=[1.0, 0.0, 0.0, 0.0], filters=Filters(), top_k=3)

    ids = [r.product_id for r in results]
    # point A and C are most similar to [1,0,0,0]
    assert ids[0] in {"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "cccccccc-cccc-cccc-cccc-cccccccccccc"}


async def test_active_status_filter_excludes_out_of_stock(qdrant_url: str) -> None:
    client = QdrantSearchClient(url=qdrant_url, collection=COLLECTION)
    results = await client.search(vector=[0.9, 0.1, 0.0, 0.0], filters=Filters(), top_k=10)

    ids = [r.product_id for r in results]
    assert "cccccccc-cccc-cccc-cccc-cccccccccccc" not in ids


async def test_price_max_filter_narrows_results(qdrant_url: str) -> None:
    client = QdrantSearchClient(url=qdrant_url, collection=COLLECTION)
    results = await client.search(
        vector=[0.0, 1.0, 0.0, 0.0],
        filters=Filters(price_max=100.0),
        top_k=10,
    )

    for r in results:
        assert r.product_id != "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"

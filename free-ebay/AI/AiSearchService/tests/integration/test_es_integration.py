import pytest

from clients.es_client import ElasticsearchClient
from models import Filters, ParsedQuery
from tests.integration.conftest import drop_es_index, seed_es_index

INDEX = "test_products_es"

_PRODUCTS = [
    {"id": "p1", "name": "Mechanical Keyboard", "description": "tactile switches", "category": "keyboards", "price": 80.0},
    {"id": "p2", "name": "Gaming Mouse", "description": "high dpi optical sensor", "category": "mice", "price": 40.0},
    {"id": "p3", "name": "Expensive Monitor", "description": "4K display", "category": "monitors", "price": 500.0},
]


@pytest.fixture(autouse=True)
async def es_index(es_url: str):
    await seed_es_index(es_url, INDEX, _PRODUCTS)
    yield
    await drop_es_index(es_url, INDEX)


def _parsed(keywords: list[str], price_max: float | None = None) -> ParsedQuery:
    return ParsedQuery(
        semantic_query=" ".join(keywords),
        filters=Filters(price_max=price_max),
        keywords=keywords,
        confidence=0.8,
        raw_query=" ".join(keywords),
    )


async def test_keyword_search_returns_matching_products(es_url: str) -> None:
    client = ElasticsearchClient(url=es_url, index=INDEX)
    results = await client.search(_parsed(["keyboard"]), top_k=10)

    ids = [r.product_id for r in results]
    assert "p1" in ids


async def test_price_max_filter_excludes_expensive_products(es_url: str) -> None:
    client = ElasticsearchClient(url=es_url, index=INDEX)
    results = await client.search(_parsed(["monitor"], price_max=100.0), top_k=10)

    ids = [r.product_id for r in results]
    assert "p3" not in ids


async def test_no_results_for_unrelated_query(es_url: str) -> None:
    client = ElasticsearchClient(url=es_url, index=INDEX)
    results = await client.search(_parsed(["zxqjfaketerm12345"]), top_k=10)

    assert results == []

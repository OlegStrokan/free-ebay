from unittest.mock import AsyncMock

import pytest

from indexer import Indexer, build_product_corpus
from models import ProductAttribute, ProductEvent


def _event(**kwargs) -> ProductEvent:
    defaults = dict(
        product_id="prod-1",
        name="Mechanical Keyboard",
        description="tactile switches",
        category="keyboards",
        price=80.0,
        currency="USD",
        stock_quantity=5,
        image_urls=["http://img.com/kb.jpg"],
        attributes=[],
    )
    defaults.update(kwargs)
    return ProductEvent(**defaults)


# ---------------------------------------------------------------------------
# build_product_corpus
# ---------------------------------------------------------------------------

def test_build_corpus_includes_name_description_category() -> None:
    event = _event(name="Keyboard", description="tactile", category="peripherals")
    corpus = build_product_corpus(event)
    assert "Keyboard" in corpus
    assert "tactile" in corpus
    assert "peripherals" in corpus


def test_build_corpus_includes_attributes_as_key_value() -> None:
    event = _event(attributes=[
        ProductAttribute(key="color", value="black"),
        ProductAttribute(key="layout", value="tenkeyless"),
    ])
    corpus = build_product_corpus(event)
    assert "color: black" in corpus
    assert "layout: tenkeyless" in corpus


def test_build_corpus_skips_none_description() -> None:
    event = _event(description=None)
    corpus = build_product_corpus(event)
    # Should not raise and should not contain literal "None"
    assert "None" not in corpus


def test_build_corpus_empty_attributes_omits_attribute_lines() -> None:
    event = _event(attributes=[])
    corpus = build_product_corpus(event)
    assert ":" not in corpus.replace("http", "")


# ---------------------------------------------------------------------------
# Indexer.upsert
# ---------------------------------------------------------------------------

@pytest.fixture
def indexer() -> Indexer:
    embedding = AsyncMock()
    embedding.embed.return_value = [0.1, 0.2, 0.3, 0.4]
    qdrant = AsyncMock()
    return Indexer(embedding, qdrant)


async def test_upsert_calls_embed_with_corpus(indexer: Indexer) -> None:
    event = _event()
    await indexer.upsert(event.model_dump())
    indexer._embedding.embed.assert_called_once()
    text = indexer._embedding.embed.call_args.args[0]
    assert isinstance(text, str)
    assert "Mechanical Keyboard" in text


async def test_upsert_uses_returned_vector(indexer: Indexer) -> None:
    indexer._embedding.embed.return_value = [0.9, 0.8, 0.7, 0.6]
    await indexer.upsert(_event().model_dump())

    _product_id, vector, _payload = indexer.qdrant.upsert.call_args.args
    assert vector == [0.9, 0.8, 0.7, 0.6]


async def test_upsert_sets_status_active_when_stock_positive(indexer: Indexer) -> None:
    await indexer.upsert(_event(stock_quantity=3).model_dump())

    _product_id, _vector, payload = indexer.qdrant.upsert.call_args.args
    assert payload["status"] == "active"


async def test_upsert_sets_status_out_of_stock_when_stock_zero(indexer: Indexer) -> None:
    await indexer.upsert(_event(stock_quantity=0).model_dump())

    _product_id, _vector, payload = indexer.qdrant.upsert.call_args.args
    assert payload["status"] == "out_of_stock"


async def test_upsert_extracts_color_brand_layout_from_attributes(indexer: Indexer) -> None:
    attrs = [
        ProductAttribute(key="color", value="black"),
        ProductAttribute(key="brand", value="Corsair"),
        ProductAttribute(key="layout", value="tenkeyless"),
    ]
    await indexer.upsert(_event(attributes=attrs).model_dump())

    _product_id, _vector, payload = indexer.qdrant.upsert.call_args.args
    assert payload["color"] == "black"
    assert payload["brand"] == "Corsair"
    assert payload["layout"] == "tenkeyless"


# ---------------------------------------------------------------------------
# Indexer.delete
# ---------------------------------------------------------------------------

async def test_delete_delegates_to_qdrant(indexer: Indexer) -> None:
    await indexer.delete("prod-42")
    indexer.qdrant.delete.assert_called_once_with("prod-42")

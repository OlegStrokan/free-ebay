from unittest.mock import AsyncMock

import pytest

from indexer import Indexer, build_product_corpus, build_catalog_item_corpus
from models import ProductAttribute, ProductEvent, CatalogItemEvent, CatalogItemIdPayload, CategoryIdPayload, CatalogItemListingSummaryEvent


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


async def test_upsert_sets_product_type_to_product(indexer: Indexer) -> None:
    await indexer.upsert(_event().model_dump())
    _product_id, _vector, payload = indexer.qdrant.upsert.call_args.args
    assert payload["product_type"] == "product"


# ---------------------------------------------------------------------------
# build_catalog_item_corpus
# ---------------------------------------------------------------------------

def _catalog_item_event(**kwargs) -> CatalogItemEvent:
    defaults = dict(
        CatalogItemId=CatalogItemIdPayload(Value="ci-1"),
        Name="iPhone 16 Pro Max",
        Description="Apple flagship smartphone",
        CategoryId=CategoryIdPayload(Value="smartphones"),
        Gtin=None,
        Attributes=[],
        ImageUrls=[],
    )
    defaults.update(kwargs)
    return CatalogItemEvent(**defaults)


def test_build_catalog_item_corpus_includes_name_and_description() -> None:
    event = _catalog_item_event()
    corpus = build_catalog_item_corpus(event)
    assert "iPhone 16 Pro Max" in corpus
    assert "Apple flagship smartphone" in corpus


def test_build_catalog_item_corpus_includes_attributes() -> None:
    event = _catalog_item_event(Attributes=[
        ProductAttribute(key="color", value="titanium"),
        ProductAttribute(key="brand", value="Apple"),
    ])
    corpus = build_catalog_item_corpus(event)
    assert "color: titanium" in corpus
    assert "brand: Apple" in corpus


def test_build_catalog_item_corpus_skips_none_description() -> None:
    event = _catalog_item_event(Description=None)
    corpus = build_catalog_item_corpus(event)
    assert "None" not in corpus


# ---------------------------------------------------------------------------
# Indexer.upsert_catalog_item
# ---------------------------------------------------------------------------

async def test_upsert_catalog_item_sets_product_type_to_catalog_item(indexer: Indexer) -> None:
    await indexer.upsert_catalog_item(_catalog_item_event().model_dump())
    _id, _vector, payload = indexer.qdrant.upsert.call_args.args
    assert payload["product_type"] == "catalog_item"


async def test_upsert_catalog_item_initial_status_is_out_of_stock(indexer: Indexer) -> None:
    await indexer.upsert_catalog_item(_catalog_item_event().model_dump())
    _id, _vector, payload = indexer.qdrant.upsert.call_args.args
    assert payload["status"] == "out_of_stock"


async def test_upsert_catalog_item_initial_min_price_is_none(indexer: Indexer) -> None:
    await indexer.upsert_catalog_item(_catalog_item_event().model_dump())
    _id, _vector, payload = indexer.qdrant.upsert.call_args.args
    assert payload["min_price"] is None
    assert payload["min_price_currency"] is None


async def test_upsert_catalog_item_initial_has_active_listings_is_false(indexer: Indexer) -> None:
    await indexer.upsert_catalog_item(_catalog_item_event().model_dump())
    _id, _vector, payload = indexer.qdrant.upsert.call_args.args
    assert payload["has_active_listings"] is False
    assert payload["seller_count"] == 0


async def test_upsert_catalog_item_uses_catalog_item_id_as_qdrant_id(indexer: Indexer) -> None:
    await indexer.upsert_catalog_item(_catalog_item_event().model_dump())
    qdrant_id, _vector, _payload = indexer.qdrant.upsert.call_args.args
    assert qdrant_id == "ci-1"


async def test_upsert_catalog_item_extracts_attributes(indexer: Indexer) -> None:
    event = _catalog_item_event(Attributes=[
        ProductAttribute(key="color", value="black"),
        ProductAttribute(key="brand", value="Apple"),
        ProductAttribute(key="layout", value="fullsize"),
    ])
    await indexer.upsert_catalog_item(event.model_dump())
    _id, _vector, payload = indexer.qdrant.upsert.call_args.args
    assert payload["color"] == "black"
    assert payload["brand"] == "Apple"
    assert payload["layout"] == "fullsize"


# ---------------------------------------------------------------------------
# Indexer.update_listing_summary
# ---------------------------------------------------------------------------

def _summary_event(**kwargs) -> CatalogItemListingSummaryEvent:
    defaults = dict(
        CatalogItemId=CatalogItemIdPayload(Value="ci-1"),
        MinPrice=899.99,
        MinPriceCurrency="USD",
        SellerCount=3,
        HasActiveListings=True,
        BestCondition="New",
        TotalStock=47,
    )
    defaults.update(kwargs)
    return CatalogItemListingSummaryEvent(**defaults)


async def test_update_listing_summary_patches_all_fields(indexer: Indexer) -> None:
    await indexer.update_listing_summary(_summary_event().model_dump())

    indexer.qdrant.update_payload.assert_called_once()
    call_kwargs = indexer.qdrant.update_payload.call_args.kwargs
    patch = call_kwargs["patch"]
    assert patch["min_price"] == 899.99
    assert patch["min_price_currency"] == "USD"
    assert patch["seller_count"] == 3
    assert patch["has_active_listings"] is True
    assert patch["best_condition"] == "New"
    assert patch["total_stock"] == 47


async def test_update_listing_summary_sets_status_active_when_has_listings(indexer: Indexer) -> None:
    await indexer.update_listing_summary(_summary_event(HasActiveListings=True).model_dump())
    patch = indexer.qdrant.update_payload.call_args.kwargs["patch"]
    assert patch["status"] == "active"


async def test_update_listing_summary_sets_status_out_of_stock_when_no_listings(indexer: Indexer) -> None:
    await indexer.update_listing_summary(_summary_event(HasActiveListings=False).model_dump())
    patch = indexer.qdrant.update_payload.call_args.kwargs["patch"]
    assert patch["status"] == "out_of_stock"


async def test_update_listing_summary_uses_catalog_item_id(indexer: Indexer) -> None:
    await indexer.update_listing_summary(_summary_event().model_dump())
    call_kwargs = indexer.qdrant.update_payload.call_args.kwargs
    assert call_kwargs["product_id"] == "ci-1"

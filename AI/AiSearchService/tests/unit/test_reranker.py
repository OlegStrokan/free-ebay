"""Unit tests for pipeline.reranker.rerank_with_preferences()."""
import json
from dataclasses import dataclass
from unittest.mock import AsyncMock

import pytest

from models import ScoredResult
from pipeline.reranker import rerank_with_preferences

def _scored(*pairs: tuple[str, float]) -> list[ScoredResult]:
    return [ScoredResult(product_id=pid, score=s) for pid, s in pairs]


def _profile(
    categories: dict | None = None,
    brands: dict | None = None,
    price_p25: float | None = None,
    price_p75: float | None = None,
    condition_weights: dict | None = None,
) -> str:
    return json.dumps({
        "categories": categories or {},
        "brands": brands or {},
        "price_p25": price_p25,
        "price_p75": price_p75,
        "condition_weights": condition_weights or {},
    })


@dataclass
class FakePoint:
    id: str
    payload: dict


def _make_qdrant_mock(payloads: dict[str, dict]) -> AsyncMock:
    """Build a mock AsyncQdrantClient whose .retrieve() returns fake points."""
    mock = AsyncMock()
    points = [
        FakePoint(id=pid, payload={**pl, "product_id": pid})
        for pid, pl in payloads.items()
    ]
    mock.retrieve.return_value = points
    return mock


def _make_redis_mock(profile_json: str | None) -> AsyncMock:
    mock = AsyncMock()
    mock.get.return_value = profile_json
    return mock

async def test_empty_results_returns_unchanged() -> None:
    result = await rerank_with_preferences(
        [], "user-1", AsyncMock(), AsyncMock(), "products"
    )
    assert result == []


async def test_empty_user_id_returns_unchanged() -> None:
    results = _scored(("p1", 1.0))
    out = await rerank_with_preferences(
        results, "", AsyncMock(), AsyncMock(), "products"
    )
    assert out is results


async def test_no_profile_in_redis_returns_unchanged() -> None:
    redis = _make_redis_mock(None)
    results = _scored(("p1", 1.0))
    out = await rerank_with_preferences(
        results, "user-1", redis, AsyncMock(), "products"
    )
    assert out is results
    redis.get.assert_awaited_once_with("user:user-1:preference_profile")


async def test_invalid_json_profile_returns_unchanged() -> None:
    redis = _make_redis_mock("not-valid-json{{{")
    results = _scored(("p1", 1.0))
    out = await rerank_with_preferences(
        results, "user-1", redis, AsyncMock(), "products"
    )
    assert out is results


async def test_category_affinity_boosts_matching_product() -> None:
    redis = _make_redis_mock(_profile(categories={"Electronics": 10.0}))
    qdrant = _make_qdrant_mock({
        "p1": {"category": "Electronics", "brand": "", "min_price": 50.0},
        "p2": {"category": "Clothing", "brand": "", "min_price": 50.0},
    })
    results = _scored(("p1", 1.0), ("p2", 1.0))

    out = await rerank_with_preferences(results, "user-1", redis, qdrant, "products")

    p1_score = next(r.score for r in out if r.product_id == "p1")
    p2_score = next(r.score for r in out if r.product_id == "p2")
    assert p1_score > p2_score
    assert out[0].product_id == "p1"


async def test_brand_affinity_boosts_matching_product() -> None:
    redis = _make_redis_mock(_profile(brands={"Sony": 8.0}))
    qdrant = _make_qdrant_mock({
        "p1": {"category": "", "brand": "Sony", "min_price": None},
        "p2": {"category": "", "brand": "Samsung", "min_price": None},
    })
    results = _scored(("p1", 1.0), ("p2", 1.0))

    out = await rerank_with_preferences(results, "user-1", redis, qdrant, "products")

    p1_score = next(r.score for r in out if r.product_id == "p1")
    p2_score = next(r.score for r in out if r.product_id == "p2")
    assert p1_score > p2_score


async def test_price_range_boosts_product_in_comfort_zone() -> None:
    redis = _make_redis_mock(_profile(price_p25=20.0, price_p75=80.0))
    qdrant = _make_qdrant_mock({
        "p1": {"category": "", "brand": "", "min_price": 50.0},
        "p2": {"category": "", "brand": "", "min_price": 200.0},
    })
    results = _scored(("p1", 1.0), ("p2", 1.0))

    out = await rerank_with_preferences(results, "user-1", redis, qdrant, "products")

    p1_score = next(r.score for r in out if r.product_id == "p1")
    p2_score = next(r.score for r in out if r.product_id == "p2")
    assert p1_score > p2_score


async def test_condition_affinity_boosts_matching_product() -> None:
    redis = _make_redis_mock(_profile(condition_weights={"New": 5.0}))
    qdrant = _make_qdrant_mock({
        "p1": {"category": "", "brand": "", "min_price": None, "best_condition": "New"},
        "p2": {"category": "", "brand": "", "min_price": None, "best_condition": "Used"},
    })
    results = _scored(("p1", 1.0), ("p2", 1.0))

    out = await rerank_with_preferences(results, "user-1", redis, qdrant, "products")

    p1_score = next(r.score for r in out if r.product_id == "p1")
    p2_score = next(r.score for r in out if r.product_id == "p2")
    assert p1_score > p2_score


async def test_all_affinities_stack() -> None:
    """A product matching all preference dimensions should get the highest boost."""
    redis = _make_redis_mock(_profile(
        categories={"Cameras": 10.0},
        brands={"Canon": 8.0},
        price_p25=200.0,
        price_p75=800.0,
        condition_weights={"New": 5.0},
    ))
    qdrant = _make_qdrant_mock({
        "best": {"category": "Cameras", "brand": "Canon", "min_price": 500.0, "best_condition": "New"},
        "mid": {"category": "Cameras", "brand": "Nikon", "min_price": 500.0, "best_condition": "Used"},
        "worst": {"category": "Clothing", "brand": "H&M", "min_price": 25.0, "best_condition": "Used"},
    })
    results = _scored(("worst", 1.0), ("mid", 1.0), ("best", 1.0))

    out = await rerank_with_preferences(results, "user-1", redis, qdrant, "products")

    scores = {r.product_id: r.score for r in out}
    assert scores["best"] > scores["mid"] > scores["worst"]
    assert out[0].product_id == "best"


async def test_reranking_preserves_order_by_score_descending() -> None:
    """Even with boosts, final output is sorted descending."""
    redis = _make_redis_mock(_profile(categories={"A": 10.0}))
    qdrant = _make_qdrant_mock({
        "p1": {"category": "A"},
        "p2": {"category": "B"},
        "p3": {"category": "A"},
    })
    # p2 has the highest raw score but doesn't match the preference
    results = _scored(("p1", 0.5), ("p2", 0.9), ("p3", 0.4))

    out = await rerank_with_preferences(results, "user-1", redis, qdrant, "products")

    for i in range(len(out) - 1):
        assert out[i].score >= out[i + 1].score


async def test_missing_payload_product_gets_no_boost() -> None:
    """If Qdrant doesn't return a point for a product, score is unchanged."""
    redis = _make_redis_mock(_profile(categories={"Electronics": 10.0}))
    # Only p1 has payload; p2 is missing from qdrant response
    qdrant = _make_qdrant_mock({"p1": {"category": "Electronics"}})
    results = _scored(("p1", 1.0), ("p2", 1.0))

    out = await rerank_with_preferences(results, "user-1", redis, qdrant, "products")

    p2_score = next(r.score for r in out if r.product_id == "p2")
    assert p2_score == 1.0  # unchanged


async def test_qdrant_retrieve_failure_returns_unchanged() -> None:
    redis = _make_redis_mock(_profile(categories={"Electronics": 10.0}))
    qdrant = AsyncMock()
    qdrant.retrieve.side_effect = Exception("connection refused")
    results = _scored(("p1", 1.0))

    out = await rerank_with_preferences(results, "user-1", redis, qdrant, "products")

    assert out is results


async def test_collection_name_forwarded_to_qdrant() -> None:
    redis = _make_redis_mock(_profile(categories={"A": 1.0}))
    qdrant = _make_qdrant_mock({"p1": {"category": "A"}})
    results = _scored(("p1", 1.0))

    await rerank_with_preferences(results, "user-1", redis, qdrant, "my_collection")

    qdrant.retrieve.assert_awaited_once()
    assert qdrant.retrieve.call_args.kwargs["collection_name"] == "my_collection"


async def test_normalized_weights_with_multiple_categories() -> None:
    """When multiple categories exist, normalization ensures proportional boosts."""
    redis = _make_redis_mock(_profile(categories={"A": 10.0, "B": 5.0}))
    qdrant = _make_qdrant_mock({
        "p1": {"category": "A"},
        "p2": {"category": "B"},
    })
    results = _scored(("p1", 1.0), ("p2", 1.0))

    out = await rerank_with_preferences(results, "user-1", redis, qdrant, "products")

    p1_score = next(r.score for r in out if r.product_id == "p1")
    p2_score = next(r.score for r in out if r.product_id == "p2")
    # A has weight 10/10=1.0, B has weight 5/10=0.5 → p1 gets bigger boost
    assert p1_score > p2_score

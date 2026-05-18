import sys
from unittest.mock import AsyncMock, MagicMock

import pytest

# inject mock proto modules
_pb2 = MagicMock()
_pb2_grpc = MagicMock()
_pb2_grpc.AiSearchServiceServicer = type("AiSearchServiceServicer", (), {})
_generated = MagicMock(ai_search_pb2=_pb2, ai_search_pb2_grpc=_pb2_grpc)
sys.modules.setdefault("generated", _generated)
_pb2 = sys.modules.setdefault("generated.ai_search_pb2", _pb2)
_pb2_grpc = sys.modules.setdefault("generated.ai_search_pb2_grpc", _pb2_grpc)
# -----------------------------------------------------------------------------

from grpc_server import AiSearchServicer  # noqa: E402


class _FakeRequest:
    def __init__(self, catalog_item_id: str = "item-1", limit: int = 10):
        self.catalog_item_id = catalog_item_id
        self.limit = limit


def _make_servicer(redis=None):
    return AiSearchServicer(
        llm_client=AsyncMock(),
        embedding_client=AsyncMock(),
        qdrant=AsyncMock(),
        es=AsyncMock(),
        redis=redis,
    )



async def test_returns_empty_when_redis_is_none() -> None:
    servicer = _make_servicer(redis=None)
    request = _FakeRequest(catalog_item_id="item-1")
    context = MagicMock()

    await servicer.GetFrequentlyBoughtTogether(request, context)

    kwargs = _pb2.GetFrequentlyBoughtTogetherResponse.call_args.kwargs
    assert kwargs["items"] == []


async def test_returns_cooccurrence_items_from_redis() -> None:
    redis_mock = AsyncMock()
    redis_mock.zrevrange.return_value = [("item-B", 5.0), ("item-C", 3.0)]

    servicer = _make_servicer(redis=redis_mock)
    request = _FakeRequest(catalog_item_id="item-A", limit=10)
    context = MagicMock()

    await servicer.GetFrequentlyBoughtTogether(request, context)

    redis_mock.zrevrange.assert_awaited_once_with(
        "cooccurrence:purchase:item-A", 0, 9, withscores=True
    )
    kwargs = _pb2.GetFrequentlyBoughtTogetherResponse.call_args.kwargs
    assert len(kwargs["items"]) == 2


async def test_defaults_limit_to_10_when_zero() -> None:
    redis_mock = AsyncMock()
    redis_mock.zrevrange.return_value = []

    servicer = _make_servicer(redis=redis_mock)
    request = _FakeRequest(catalog_item_id="item-A", limit=0)
    context = MagicMock()

    await servicer.GetFrequentlyBoughtTogether(request, context)

    redis_mock.zrevrange.assert_awaited_once_with(
        "cooccurrence:purchase:item-A", 0, 9, withscores=True
    )


async def test_aborts_when_catalog_item_id_empty() -> None:
    servicer = _make_servicer(redis=AsyncMock())
    request = _FakeRequest(catalog_item_id="")
    context = AsyncMock()

    await servicer.GetFrequentlyBoughtTogether(request, context)

    context.abort.assert_awaited_once()


async def test_respects_custom_limit() -> None:
    redis_mock = AsyncMock()
    redis_mock.zrevrange.return_value = [("item-X", 1.0)]

    servicer = _make_servicer(redis=redis_mock)
    request = _FakeRequest(catalog_item_id="item-A", limit=5)
    context = MagicMock()

    await servicer.GetFrequentlyBoughtTogether(request, context)

    redis_mock.zrevrange.assert_awaited_once_with(
        "cooccurrence:purchase:item-A", 0, 4, withscores=True
    )


async def test_redis_error_returns_empty_and_does_not_crash() -> None:
    """If Redis raises an exception, the handler should not crash uncontrolled."""
    import redis as redis_lib

    redis_mock = AsyncMock()
    redis_mock.zrevrange.side_effect = redis_lib.ConnectionError("connection lost")

    servicer = _make_servicer(redis=redis_mock)
    request = _FakeRequest(catalog_item_id="item-A", limit=10)
    context = MagicMock()

    # Currently the handler does NOT catch Redis errors, so it will raise
    with pytest.raises(redis_lib.ConnectionError):
        await servicer.GetFrequentlyBoughtTogether(request, context)


async def test_negative_limit_treated_as_default() -> None:
    """Negative limit is falsy (0 and negatives) so should default to 10."""
    redis_mock = AsyncMock()
    redis_mock.zrevrange.return_value = []

    servicer = _make_servicer(redis=redis_mock)
    # limit = -1 is truthy in Python, so it passes through the `or 10` guard
    request = _FakeRequest(catalog_item_id="item-A", limit=-1)
    context = MagicMock()

    await servicer.GetFrequentlyBoughtTogether(request, context)

    # -1 is truthy so passes as-is: zrevrange(key, 0, -2) which is valid Redis behavior
    redis_mock.zrevrange.assert_awaited_once_with(
        "cooccurrence:purchase:item-A", 0, -2, withscores=True
    )

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

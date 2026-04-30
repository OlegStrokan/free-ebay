"""
Unit tests for AiSearchServicer.Search().

The gRPC generated stubs (generated.ai_search_pb2 / ai_search_pb2_grpc) are
code-generated artefacts that may not exist in a fresh checkout.  We inject
mock modules into sys.modules before importing grpc_server so the tests run
without a build step.  If the real stubs are present they will NOT be replaced
(setdefault semantics).
"""
import json
import sys
from unittest.mock import AsyncMock, MagicMock, patch

import pytest

# ---------- inject mock proto modules ----------------------------------------
_pb2 = MagicMock()
_pb2_grpc = MagicMock()
# The servicer base class MUST be a real class (not MagicMock) so that
# Python's MRO doesn't shadow async methods defined in subclasses.
_pb2_grpc.AiSearchServiceServicer = type("AiSearchServiceServicer", (), {})
_generated = MagicMock(ai_search_pb2=_pb2, ai_search_pb2_grpc=_pb2_grpc)
sys.modules.setdefault("generated", _generated)
sys.modules.setdefault("generated.ai_search_pb2", _pb2)
sys.modules.setdefault("generated.ai_search_pb2_grpc", _pb2_grpc)
# -----------------------------------------------------------------------------

from grpc_server import AiSearchServicer  # noqa: E402 — must come after injection
from models import Filters, ParsedQuery, SearchPipelineResult, SearchResultItem


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

class _FakeRequest:
    def __init__(self, query: str = "keyboard", page: int = 1, page_size: int = 20, debug: bool = False):
        self.query = query
        self.page = page
        self.page_size = page_size
        self.debug = debug


def _make_servicer():
    return AiSearchServicer(
        llm_client=AsyncMock(),
        embedding_client=AsyncMock(),
        qdrant=AsyncMock(),
        es=AsyncMock(),
    )


_PIPELINE_RESULT = SearchPipelineResult(
    items=[
        SearchResultItem(
            product_id="p1",
            name="Keyboard",
            category="Electronics",
            price=49.99,
            currency="USD",
            relevance_score=0.95,
            image_urls=["http://example.com/img.jpg"],
        )
    ],
    total=1,
    parsed_query=ParsedQuery(
        semantic_query="keyboard",
        filters=Filters(),
        keywords=["keyboard"],
        confidence=0.9,
        raw_query="keyboard",
    ),
    used_ai=True,
)

_EMPTY_RESULT = SearchPipelineResult(
    items=[], total=0, parsed_query=None, used_ai=False
)


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

async def test_search_passes_page_and_page_size_to_pipeline() -> None:
    servicer = _make_servicer()
    request = _FakeRequest(query="test", page=3, page_size=15)

    with patch("grpc_server.run_search_pipeline", new=AsyncMock(return_value=_PIPELINE_RESULT)) as mock_pipeline:
        await servicer.Search(request, MagicMock())

    assert mock_pipeline.call_args.kwargs["page"] == 3
    assert mock_pipeline.call_args.kwargs["page_size"] == 15


async def test_search_defaults_page_to_1_and_page_size_to_20_when_zero() -> None:
    servicer = _make_servicer()
    request = _FakeRequest(page=0, page_size=0)

    with patch("grpc_server.run_search_pipeline", new=AsyncMock(return_value=_PIPELINE_RESULT)) as mock_pipeline:
        await servicer.Search(request, MagicMock())

    assert mock_pipeline.call_args.kwargs["page"] == 1
    assert mock_pipeline.call_args.kwargs["page_size"] == 20


async def test_search_debug_false_produces_empty_debug_string() -> None:
    servicer = _make_servicer()
    request = _FakeRequest(debug=False)

    with patch("grpc_server.run_search_pipeline", new=AsyncMock(return_value=_PIPELINE_RESULT)):
        await servicer.Search(request, MagicMock())

    kwargs = _pb2.SearchResponse.call_args.kwargs
    assert kwargs["parsed_query_debug"] == ""


async def test_search_debug_true_produces_json_with_correct_fields() -> None:
    servicer = _make_servicer()
    request = _FakeRequest(debug=True)

    with patch("grpc_server.run_search_pipeline", new=AsyncMock(return_value=_PIPELINE_RESULT)):
        await servicer.Search(request, MagicMock())

    kwargs = _pb2.SearchResponse.call_args.kwargs
    data = json.loads(kwargs["parsed_query_debug"])
    assert data["semantic_query"] == "keyboard"
    assert data["confidence"] == 0.9
    assert "keywords" in data


async def test_search_used_ai_forwarded_from_pipeline_result() -> None:
    servicer = _make_servicer()
    request = _FakeRequest()

    with patch("grpc_server.run_search_pipeline", new=AsyncMock(return_value=_EMPTY_RESULT)):
        await servicer.Search(request, MagicMock())

    kwargs = _pb2.SearchResponse.call_args.kwargs
    assert kwargs["used_ai"] is False
    assert kwargs["total_count"] == 0

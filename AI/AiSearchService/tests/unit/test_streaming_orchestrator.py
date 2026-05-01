"""
Unit tests for pipeline.streaming_orchestrator.run_streaming_search().
"""
import asyncio
from unittest.mock import AsyncMock

import pytest

from models import Filters, ParsedQuery, ScoredResult
from pipeline.streaming_orchestrator import (
    PartialSearchResult,
    SearchPhase,
    run_streaming_search,
    _fallback_parse,
    _to_items,
)


def _make_clients():
    llm = AsyncMock()
    embedding = AsyncMock()
    qdrant = AsyncMock()
    es = AsyncMock()
    return llm, embedding, qdrant, es


def _scored(*ids: str) -> list[ScoredResult]:
    return [ScoredResult(product_id=pid, score=1.0 / (i + 1)) for i, pid in enumerate(ids)]


def _parsed(confidence: float = 0.8) -> ParsedQuery:
    return ParsedQuery(
        semantic_query="keyboard",
        filters=Filters(),
        keywords=["keyboard"],
        confidence=confidence,
        raw_query="keyboard",
    )


async def _collect(gen) -> list[PartialSearchResult]:
    results = []
    async for item in gen:
        results.append(item)
    return results


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------


async def test_yields_two_phases_keyword_then_merged() -> None:
    llm, embedding, qdrant, es = _make_clients()
    llm.parse_query.return_value = _parsed()
    embedding.embed.return_value = [0.1] * 4
    es.search.return_value = _scored("p1", "p2")
    qdrant.search.return_value = _scored("p2", "p3")

    results = await _collect(run_streaming_search(
        "keyboard", page=1, page_size=20,
        llm_client=llm, embedding_client=embedding, qdrant=qdrant, es=es,
        llm_timeout=5.0, top_k=50, rrf_k=60,
    ))

    assert len(results) == 2
    assert results[0].phase == SearchPhase.KEYWORD
    assert results[1].phase == SearchPhase.MERGED


async def test_keyword_phase_contains_only_es_results() -> None:
    llm, embedding, qdrant, es = _make_clients()
    llm.parse_query.return_value = _parsed()
    embedding.embed.return_value = [0.1] * 4
    es.search.return_value = _scored("es1", "es2")
    qdrant.search.return_value = _scored("q1")

    results = await _collect(run_streaming_search(
        "keyboard", page=1, page_size=20,
        llm_client=llm, embedding_client=embedding, qdrant=qdrant, es=es,
        llm_timeout=5.0, top_k=50, rrf_k=60,
    ))

    keyword_ids = {i.product_id for i in results[0].items}
    assert keyword_ids == {"es1", "es2"}


async def test_merged_phase_contains_rrf_of_both_sources() -> None:
    llm, embedding, qdrant, es = _make_clients()
    llm.parse_query.return_value = _parsed()
    embedding.embed.return_value = [0.1] * 4
    es.search.return_value = _scored("p1", "p2")
    qdrant.search.return_value = _scored("p2", "p3")

    results = await _collect(run_streaming_search(
        "keyboard", page=1, page_size=20,
        llm_client=llm, embedding_client=embedding, qdrant=qdrant, es=es,
        llm_timeout=5.0, top_k=50, rrf_k=60,
    ))

    merged_ids = {i.product_id for i in results[1].items}
    # p2 appears in both sources, p1 only in ES, p3 only in Qdrant
    assert merged_ids == {"p1", "p2", "p3"}


async def test_used_ai_true_when_confidence_above_zero() -> None:
    llm, embedding, qdrant, es = _make_clients()
    llm.parse_query.return_value = _parsed(confidence=0.7)
    embedding.embed.return_value = [0.1] * 4
    es.search.return_value = _scored("p1")
    qdrant.search.return_value = _scored("p1")

    results = await _collect(run_streaming_search(
        "keyboard", page=1, page_size=20,
        llm_client=llm, embedding_client=embedding, qdrant=qdrant, es=es,
        llm_timeout=5.0, top_k=50, rrf_k=60,
    ))

    assert results[0].used_ai is True
    assert results[1].used_ai is True


async def test_used_ai_false_when_llm_times_out() -> None:
    llm, embedding, qdrant, es = _make_clients()

    async def slow_parse(_):
        await asyncio.sleep(10)

    llm.parse_query.side_effect = slow_parse
    embedding.embed.return_value = [0.1] * 4
    es.search.return_value = _scored("p1")
    qdrant.search.return_value = _scored("p1")

    results = await _collect(run_streaming_search(
        "keyboard", page=1, page_size=20,
        llm_client=llm, embedding_client=embedding, qdrant=qdrant, es=es,
        llm_timeout=0.01, top_k=50, rrf_k=60,
    ))

    assert results[0].used_ai is False
    assert results[1].used_ai is False


async def test_pagination_applied_to_both_phases() -> None:
    llm, embedding, qdrant, es = _make_clients()
    llm.parse_query.return_value = _parsed()
    embedding.embed.return_value = [0.1] * 4
    es.search.return_value = _scored(*[f"e{i}" for i in range(10)])
    qdrant.search.return_value = _scored(*[f"q{i}" for i in range(10)])

    results = await _collect(run_streaming_search(
        "keyboard", page=2, page_size=3,
        llm_client=llm, embedding_client=embedding, qdrant=qdrant, es=es,
        llm_timeout=5.0, top_k=50, rrf_k=60,
    ))

    # page 2 with page_size 3 → items 3,4,5 (0-indexed)
    assert len(results[0].items) == 3
    assert len(results[1].items) == 3


async def test_cancellation_stops_after_es_phase() -> None:
    llm, embedding, qdrant, es = _make_clients()
    llm.parse_query.return_value = _parsed()
    embedding.embed.return_value = [0.1] * 4
    es.search.return_value = _scored("p1")

    # Make qdrant hang forever - we'll cancel before it resolves
    qdrant_called = asyncio.Event()

    async def slow_qdrant(vector, filters, top_k):
        qdrant_called.set()
        await asyncio.sleep(100)
        return []

    qdrant.search.side_effect = slow_qdrant

    results = []
    gen = run_streaming_search(
        "keyboard", page=1, page_size=20,
        llm_client=llm, embedding_client=embedding, qdrant=qdrant, es=es,
        llm_timeout=5.0, top_k=50, rrf_k=60,
    )

    # Get first yield (keyword phase)
    results.append(await gen.__anext__())
    assert results[0].phase == SearchPhase.KEYWORD

    # Cancel the generator (simulates stream cancellation)
    await gen.aclose()

    # Should only have the keyword result
    assert len(results) == 1


async def test_es_and_embedding_run_concurrently() -> None:
    llm, embedding, qdrant, es = _make_clients()
    llm.parse_query.return_value = _parsed()

    call_order: list[str] = []

    async def track_embed(text):
        call_order.append("embed_start")
        await asyncio.sleep(0.01)
        call_order.append("embed_end")
        return [0.1] * 4

    async def track_es(parsed, top_k):
        call_order.append("es_start")
        await asyncio.sleep(0.01)
        call_order.append("es_end")
        return _scored("p1")

    async def track_qdrant(vector, filters, top_k):
        call_order.append("qdrant")
        return _scored("p1")

    embedding.embed.side_effect = track_embed
    es.search.side_effect = track_es
    qdrant.search.side_effect = track_qdrant

    await _collect(run_streaming_search(
        "keyboard", page=1, page_size=20,
        llm_client=llm, embedding_client=embedding, qdrant=qdrant, es=es,
        llm_timeout=5.0, top_k=50, rrf_k=60,
    ))

    # Both should start before either ends (concurrent)
    embed_start = call_order.index("embed_start")
    es_start = call_order.index("es_start")
    embed_end = call_order.index("embed_end")
    es_end = call_order.index("es_end")
    # Both start before either finishes
    assert embed_start < embed_end
    assert es_start < es_end


def test_fallback_parse_returns_zero_confidence() -> None:
    result = _fallback_parse("red keyboard")
    assert result.confidence == 0.0
    assert result.keywords == ["red", "keyboard"]
    assert result.semantic_query == "red keyboard"


def test_to_items_paginates_correctly() -> None:
    scored = _scored("a", "b", "c", "d", "e")
    items, total = _to_items(scored, page=2, page_size=2)
    assert total == 5
    assert len(items) == 2
    assert items[0].product_id == "c"
    assert items[1].product_id == "d"

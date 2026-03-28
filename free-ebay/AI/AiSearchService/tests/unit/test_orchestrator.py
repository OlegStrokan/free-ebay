import asyncio
from unittest.mock import AsyncMock, patch

import pytest

from models import Filters, ParsedQuery, ScoredResult, SearchPipelineResult
from pipeline.orchestrator import fallback_parse, run_search_pipeline


def _make_clients():
    llm = AsyncMock()
    embedding = AsyncMock()
    qdrant = AsyncMock()
    es = AsyncMock()
    return llm, embedding, qdrant, es


def _scored(*ids: str) -> list[ScoredResult]:
    return [ScoredResult(product_id=pid, score=1.0) for pid in ids]


async def test_used_ai_true_when_llm_returns_positive_confidence() -> None:
    llm, embedding, qdrant, es = _make_clients()
    llm.parse_query.return_value = ParsedQuery(
        semantic_query="keyboard", filters=Filters(), keywords=["keyboard"],
        confidence=0.8, raw_query="keyboard",
    )
    embedding.embed.return_value = [0.1] * 4
    qdrant.search.return_value = _scored("p1")
    es.search.return_value = _scored("p2")

    result = await run_search_pipeline(
        "keyboard", page=1, page_size=20,
        llm_client=llm, embedding_client=embedding, qdrant=qdrant, es=es,
        llm_timeout=5.0, top_k=50, rrf_k=60,
    )

    assert result.used_ai is True


async def test_used_ai_false_when_llm_times_out() -> None:
    llm, embedding, qdrant, es = _make_clients()

    async def slow_parse(_):
        await asyncio.sleep(10)

    llm.parse_query.side_effect = slow_parse
    embedding.embed.return_value = [0.1] * 4
    qdrant.search.return_value = []
    es.search.return_value = []

    result = await run_search_pipeline(
        "keyboard", page=1, page_size=20,
        llm_client=llm, embedding_client=embedding, qdrant=qdrant, es=es,
        llm_timeout=0.01, top_k=50, rrf_k=60,
    )

    assert result.used_ai is False


async def test_pagination_slices_merged_results_correctly() -> None:
    llm, embedding, qdrant, es = _make_clients()
    llm.parse_query.return_value = ParsedQuery(
        semantic_query="q", filters=Filters(), keywords=[], confidence=0.5, raw_query="q",
    )
    embedding.embed.return_value = [0.1] * 4
    # 10 distinct products in each source → 20 unique after RRF
    qdrant.search.return_value = _scored(*[f"q{i}" for i in range(10)])
    es.search.return_value = _scored(*[f"e{i}" for i in range(10)])

    result_p1 = await run_search_pipeline(
        "q", page=1, page_size=5,
        llm_client=llm, embedding_client=embedding, qdrant=qdrant, es=es,
        llm_timeout=5.0, top_k=50, rrf_k=60,
    )
    result_p2 = await run_search_pipeline(
        "q", page=2, page_size=5,
        llm_client=llm, embedding_client=embedding, qdrant=qdrant, es=es,
        llm_timeout=5.0, top_k=50, rrf_k=60,
    )

    assert len(result_p1.items) == 5
    assert len(result_p2.items) == 5
    p1_ids = {i.product_id for i in result_p1.items}
    p2_ids = {i.product_id for i in result_p2.items}
    assert p1_ids.isdisjoint(p2_ids)


async def test_embedding_and_es_tasks_are_launched_before_qdrant() -> None:
    """Ensures embedding and ES fan-out start concurrently before Qdrant is called."""
    llm, embedding, qdrant, es = _make_clients()
    call_order: list[str] = []

    async def track_embed(text):
        call_order.append("embed")
        return [0.1] * 4

    async def track_es(parsed, top_k):
        call_order.append("es")
        return []

    async def track_qdrant(vector, filters, top_k):
        call_order.append("qdrant")
        return []

    llm.parse_query.return_value = ParsedQuery(
        semantic_query="q", filters=Filters(), keywords=[], confidence=0.5, raw_query="q",
    )
    embedding.embed.side_effect = track_embed
    es.search.side_effect = track_es
    qdrant.search.side_effect = track_qdrant

    await run_search_pipeline(
        "q", page=1, page_size=10,
        llm_client=llm, embedding_client=embedding, qdrant=qdrant, es=es,
        llm_timeout=5.0, top_k=50, rrf_k=60,
    )

    # embed and es must both be called before or together with qdrant
    qdrant_index = call_order.index("qdrant")
    assert "embed" in call_order[:qdrant_index + 1]
    assert "es" in call_order[:qdrant_index + 1]


def test_fallback_parse_returns_zero_confidence() -> None:
    result = fallback_parse("red keyboard")
    assert result.confidence == 0.0
    assert result.keywords == ["red", "keyboard"]
    assert result.semantic_query == "red keyboard"

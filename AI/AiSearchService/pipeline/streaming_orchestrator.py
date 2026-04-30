import asyncio
import structlog
from collections.abc import AsyncGenerator
from dataclasses import dataclass
from enum import Enum

from models import (
    Filters,
    ParsedQuery,
    SearchResultItem,
    ScoredResult,
)
from clients.embedding_client import EmbeddingClient
from clients.llm_query_client import LLMQueryClient
from clients.qdrant_client import QdrantSearchClient
from clients.es_client import ElasticsearchClient
from pipeline.rrf import rrf_merge

log = structlog.get_logger()


class SearchPhase(Enum):
    KEYWORD = "keyword"
    MERGED = "merged"


@dataclass
class PartialSearchResult:
    phase: SearchPhase
    items: list[SearchResultItem]
    total: int
    used_ai: bool


def _fallback_parse(query: str) -> ParsedQuery:
    return ParsedQuery(
        semantic_query=query,
        filters=Filters(),
        keywords=query.split(),
        confidence=0.0,
        raw_query=query,
    )


def _to_items(
    results: list[ScoredResult], page: int, page_size: int
) -> tuple[list[SearchResultItem], int]:
    start = (page - 1) * page_size
    page_slice = results[start : start + page_size]
    items = [
        SearchResultItem(
            product_id=r.product_id,
            name="",
            category="",
            price=0.0,
            currency="USD",
            relevance_score=r.score,
            image_urls=[],
        )
        for r in page_slice
    ]
    return items, len(results)


async def run_streaming_search(
    query: str,
    page: int,
    page_size: int,
    llm_client: LLMQueryClient,
    embedding_client: EmbeddingClient,
    qdrant: QdrantSearchClient,
    es: ElasticsearchClient,
    llm_timeout: float,
    top_k: int,
    rrf_k: int,
) -> AsyncGenerator[PartialSearchResult, None]:
    """Yield partial results as they become available

    First yield:  keyword-only ES results   (+-40 ms)
    Second yield: RRF-merged ES+Qdrant results (+-1-2 s)

    Supports cooperative cancellation via ``asyncio.Task.cancel()``
    """

    # --- LLM query parsing (hard timeout) ---
    try:
        parsed = await asyncio.wait_for(
            llm_client.parse_query(query),
            timeout=llm_timeout,
        )
        log.info("stream_llm_parse_ok", confidence=parsed.confidence)
    except asyncio.TimeoutError:
        log.warning("stream_llm_parse_timeout", query=query)
        parsed = _fallback_parse(query)

    used_ai = parsed.confidence > 0.0

    # --- kick off ES + embedding in parallel ---
    es_task = asyncio.create_task(es.search(parsed, top_k=top_k))
    embedding_task = asyncio.create_task(embedding_client.embed(parsed.semantic_query))

    try:
        es_results = await es_task
    except asyncio.CancelledError:
        embedding_task.cancel()
        raise

    # --- phase 1: keyword results (fast) ---
    keyword_items, keyword_total = _to_items(es_results, page, page_size)
    yield PartialSearchResult(
        phase=SearchPhase.KEYWORD,
        items=keyword_items,
        total=keyword_total,
        used_ai=used_ai,
    )

    # --- wait for embedding, then qdrant ---
    try:
        vector = await embedding_task
    except asyncio.CancelledError:
        raise

    qdrant_results = await qdrant.search(vector, parsed.filters, top_k=top_k)

    # --- phase 2: RRF-merged results ---
    merged = rrf_merge(qdrant_results, es_results, k=rrf_k)
    merged_items, merged_total = _to_items(merged, page, page_size)
    yield PartialSearchResult(
        phase=SearchPhase.MERGED,
        items=merged_items,
        total=merged_total,
        used_ai=used_ai,
    )

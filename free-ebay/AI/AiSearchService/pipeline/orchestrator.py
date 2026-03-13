import asyncio
import json
import structlog
from models import (
    Filters,
    ParsedQuery,
    SearchPipelineResult,
    SearchResultItem,
    ScoredResult,
)
from clients.embedding_client import EmbeddingClient
from clients.llm_query_client import LLMQueryClient
from clients.qdrant_client import QdrantSearchClient
from clients.es_client import ElasticsearchClient
from pipeline.rrf import rrf_merge

log = structlog.get_logger()

def fallback_parse(query: str) -> ParsedQuery:
    return ParsedQuery(
        semantic_query=query,
        filters=Filters(),
        keywords=query.split(),
        confidence=0.0,
        raw_query=query,
    )


async def run_search_pipeline(
        query: str,
        page: int,
        page_size: int,
        llm_client: LLMQueryClient,
        embedding_client: EmbeddingClient,
        qdrant: QdrantSearchClient,
        es: ElasticsearchClient,
        llm_timeout: float,
        top_k: int,
        rrf_k: int
) -> SearchPipelineResult:

    # llm query parsing - hard timeout so slow llm never blocks search
    try:
        parsed = await asyncio.wait_for(
            llm_client.parse_query(query),
            timeout=llm_timeout,
        )
        log.info("llm_parse_success", confidence=parsed.confidence)

    except asyncio.TimeoutError:
        log.warning("llm_parse_timeout", query=query)
        parsed = fallback_parse(query)


    # embed the semantic query and run elastic keyword search in parallel, cool right?
    embedding_task = asyncio.create_task(embedding_client.embed(parsed.semantic_query))
    es_task = asyncio.create_task(es.search(parsed, top_k=top_k))

    vector = await embedding_task
    qdrant_task = asyncio.create_task(qdrant.search(vector, parsed.filters, top_k=top_k))
    qdrant_results, es_results = await asyncio.gather(qdrant_task, es_task)

    # RRF type shit
    merged: list[ScoredResult] = rrf_merge(qdrant_results, es_results, k=rrf_k)

    start = (page - 1) * page_size
    page_slice = merged[start : start + page_size]

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

    return SearchPipelineResult(
        items=items,
        total=len(merged),
        parsed_query=parsed,
        used_ai=(parsed.confidence > 0.0),
    )


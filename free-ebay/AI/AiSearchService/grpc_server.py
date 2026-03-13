import json
import asyncio
import grpc
import structlog
from generated import ai_search_pb2, ai_search_pb2_grpc
from pipeline.orchestrator import run_search_pipeline
from config import settings

log = structlog.get_logger()

class AiSearchServicer(ai_search_pb2_grpc.AiSearchServiceServicer):
    def __init__(self, llm_client, embedding_client, qdrant, es):
        self._llm = llm_client
        self._embedding = embedding_client
        self.qdrant = qdrant
        self._es = es

    async def Search(self, request, context):
        result = await run_search_pipeline(
            query=request.query,
            page=request.page or 1,
            page_size=request.page_size or 20,
            llm_client=self._llm,
            embedding_client=self._embedding,
            qdrant=self._qdrant,
            es=self._es,
            llm_timeout=settings.llm_timeout_seconds,
            top_k=settings.top_k,
            rrf_k=settings.rrf_k,
        )

        items = [
            ai_search_pb2.SearchResultItem(
                product_id=item.product_id,
                name=item.name,
                category=item.category,
                price=item.price,
                currency=item.currency,
                relevance_score=item.relevance_score,
                image_urls=item.image_urls,
            )
            for item in result.items
        ]

        parsed_debug = ""
        if request.debug and result.parsed_query:
            parsed_debug = json.dumps({
                "semantic_query": result.parsed_query.semantic_query,
                "keyword": result.parsed_query.keywords,
                "confidence": result.parsed_query.confidence,
            })

        return ai_search_pb2.SearchResponse(
            items=items,
            total_count=result.total,
            parsed_query_debug=parsed_debug,
            used_ai=result.used_ai,
        )

async def serve(servicer: AiSearchServicer) -> None:
    server = grpc.aio.server()
    ai_search_pb2_grpc.add_AiSearchServiceServicer_to_server(servicer, server)
    server.add_insecure_port(f"[::]:{settings.gprc_port}")
    await server.start()
    log.info("grpc_server_started", port=settings.gprc_port)
    await server.wait_for_termination()
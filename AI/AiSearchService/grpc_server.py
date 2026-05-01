import json
import asyncio
import grpc
import structlog
from generated import ai_search_pb2, ai_search_pb2_grpc
from pipeline.orchestrator import run_search_pipeline
from pipeline.streaming_orchestrator import run_streaming_search, SearchPhase
from config import settings

log = structlog.get_logger()

_PHASE_TO_PROTO = {
    SearchPhase.KEYWORD: ai_search_pb2.SEARCH_PHASE_KEYWORD,
    SearchPhase.MERGED: ai_search_pb2.SEARCH_PHASE_MERGED,
}


class AiSearchServicer(ai_search_pb2_grpc.AiSearchServiceServicer):
    def __init__(self, llm_client, embedding_client, qdrant, es):
        self._llm = llm_client
        self._embedding = embedding_client
        self._qdrant = qdrant
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
                "keywords": result.parsed_query.keywords,
                "confidence": result.parsed_query.confidence,
            })

        return ai_search_pb2.SearchResponse(
            items=items,
            total_count=result.total,
            parsed_query_debug=parsed_debug,
            used_ai=result.used_ai,
        )

    async def SearchStream(self, request_iterator, context):
        """Bidirectional streaming RPC for progressive search
        For each query received
          1. Cancel any previous in-flight search on this stream.
          2. Push ES keyword results as soon as ready (+-40 ms)
          3. Push RRF-merged results when Qdrant responds (+-1-2 s)
        Stream stays open until the client closes it
        """
        current_task: asyncio.Task | None = None
        current_request_id: str | None = None
        send_queue: asyncio.Queue = asyncio.Queue()

        async def _run_and_enqueue(request_id: str, query: str, page: int, page_size: int):
            try:
                async for partial in run_streaming_search(
                    query=query,
                    page=page,
                    page_size=page_size,
                    llm_client=self._llm,
                    embedding_client=self._embedding,
                    qdrant=self._qdrant,
                    es=self._es,
                    llm_timeout=settings.llm_timeout_seconds,
                    top_k=settings.top_k,
                    rrf_k=settings.rrf_k,
                ):
                    await send_queue.put((request_id, partial))
            except asyncio.CancelledError:
                log.info("stream_search_cancelled", request_id=request_id)

        async def _read_requests():
            nonlocal current_task, current_request_id
            async for req in request_iterator:
                log.info("stream_query_received", request_id=req.request_id, query=req.query)

                # cancel previous in-flight search
                if current_task and not current_task.done():
                    current_task.cancel()
                    try:
                        await current_task
                    except asyncio.CancelledError:
                        pass

                current_request_id = req.request_id
                current_task = asyncio.create_task(
                    _run_and_enqueue(
                        req.request_id,
                        req.query,
                        req.page or 1,
                        req.page_size or 20,
                    )
                )

            # client closed the stream - let final search finish
            if current_task and not current_task.done():
                await current_task
            await send_queue.put(None)  # sentinel

        reader_task = asyncio.create_task(_read_requests())

        try:
            while True:
                item = await send_queue.get()
                if item is None:
                    break

                request_id, partial = item

                # drop stale results from a cancelled search that raced
                if request_id != current_request_id:
                    continue

                proto_items = [
                    ai_search_pb2.SearchResultItem(
                        product_id=it.product_id,
                        name=it.name,
                        category=it.category,
                        price=it.price,
                        currency=it.currency,
                        relevance_score=it.relevance_score,
                        image_urls=it.image_urls,
                    )
                    for it in partial.items
                ]

                yield ai_search_pb2.StreamSearchResponse(
                    request_id=request_id,
                    phase=_PHASE_TO_PROTO[partial.phase],
                    items=proto_items,
                    total_count=partial.total,
                    used_ai=partial.used_ai,
                )
        finally:
            if current_task and not current_task.done():
                current_task.cancel()
            reader_task.cancel()

async def serve(servicer: AiSearchServicer) -> None:
    server = grpc.aio.server()
    ai_search_pb2_grpc.add_AiSearchServiceServicer_to_server(servicer, server)
    server.add_insecure_port(f"[::]:{settings.grpc_port}")
    await server.start()
    log.info("grpc_server_started", port=settings.grpc_port)
    await server.wait_for_termination()
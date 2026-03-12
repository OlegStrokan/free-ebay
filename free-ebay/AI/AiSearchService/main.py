import asyncio
import structlog
from fastapi import FastAPI
from contextlib import asynccontextmanager
from typing import AsyncIterator

from clients.embedding_client import EmbeddingClient
from clients.llm_query_client import LLMQueryClient
from clients.qdrant_client import QdrantSearchClient
from clients.es_client import ElasticsearchClient
from grpc_server import AiSearchServicer, serve
from config import settings

log = structlog.get_logger()

@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    embedding = EmbeddingClient(base_url=settings.embedding_service_url)
    llm = LLMQueryClient(base_url=settings.llm_query_service_url)
    qdrant = QdrantSearchClient(url=settings.qdrant_url, collection=settings.qdrant_collection)
    es = ElasticsearchClient(url=settings.es_url, index=settings.es_index)

    servicer = AiSearchServicer(llm, embedding, qdrant, es)
    grpc_task = asyncio.create_task(serve(servicer))

    app.state.embedding = embedding
    app.state.llm = llm
    app.state.qdrant = qdrant
    app.state.es = es

    log.info("ai_search_service_started")
    yield

    grpc_task.cancel()
    await embedding.aclose()
    await llm.aclose()
    await es.aclose()

app = FastAPI(title="AISearchService", lifespan=lifespan)

@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "ok"}

@app.get("/ready")
async def ready() -> dict[str, str]:
    return {"status": "ok"}
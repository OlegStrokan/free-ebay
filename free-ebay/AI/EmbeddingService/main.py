from contextlib import asynccontextmanager
from typing import AsyncIterator

import structlog
from fastapi import FastAPI

from clients.ollama_client import OllamaClient
from config import settings
from routes.embed import get_ollama_client
from routes.embed import router as embed_router;

log = structlog.get_logger()

@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    client = OllamaClient(base_url=settings.ollama_base_url)

    # override the dependency
    app.dependency_overrides[get_ollama_client] = lambda: client
    log.info("embedding_service_started", ollama_url=settings.ollama_base_url)
    yield
    await client.aclose()
    log.info("embedding_service_stopped")

app = FastAPI(title="EmbeddingService", lifespan=lifespan)
app.include_router(embed_router)

@app.get('/health')
async def health() -> dict[str, str]:
    return {"status": "ok"}

@app.get('/ready')
async def ready() -> dict[str, str]:
    return {"status": "ok"}

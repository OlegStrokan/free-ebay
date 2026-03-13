from contextlib import asynccontextmanager
from typing import AsyncIterator
import structlog
from fastapi import FastAPI
from clients.ollama_client import OllamaClient
from config import settings
from routes.parse import router as parse_router, get_ollama_client


log = structlog.get_logger()

@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    client = OllamaClient()
    app.dependency_overrides[get_ollama_client] = lambda: client
    log.info("llm_query_service_started", model=settings.model)
    yield
    await client.aclose()

app = FastAPI(title="LLMQueryService", lifespan=lifespan)
app.include_router(parse_router)

@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "ok"}

@app.get("/ready")
async def ready() -> dict[str, str]:
    return {"status": "ok"}
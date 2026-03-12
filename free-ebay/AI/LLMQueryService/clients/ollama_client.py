import json
from pathlib import Path
import httpx
import structlog
from models import Filters, ParsedQuery
from config import settings

log = structlog.get_logger()

PROMPT_PATH= Path(__file__).parent.parent / "prompts" / "query_extraction.txt"

def fallback_parse(query: str) -> ParsedQuery:
    return ParsedQuery(
        semantic_query=query,
        filters=Filters(),
        keywords=query.split(),
        confidence=0.0,
        raw_query=query,
    )


class OllamaClient:
    def __init__(self) -> None:
        self._http = httpx.AsyncClient(
            base_url=settings.ollama_base_url,
            timeout=settings.timeout_seconds,
        )
        self._prompt_template = PROMPT_PATH.read_text(encoding="utf-8")

    async def parse_query(self, query: str) -> ParsedQuery:
        prompt = self._prompt_template.format(query=query)
        try:
            response = await self._http.post(
                "/api/generate",
                json={
                    "model": settings.model,
                    "prompt": prompt,
                    "format": "json",
                    "stream": False,
                    "options": {
                        "temperature": settings.temperature,
                        "num_predict": settings.num_predict,
                    },
                },
            )
            response.raise_for_status()
            data = json.loads(response.json()["response"])
            parsed = ParsedQuery(**data, raw_query=query)
            return parsed
        except Exception:
            log.exception("llm_parse_failed", query=query)
            return fallback_parse(query)
    async def aclose(self) -> None:
        await self._http.aclose()
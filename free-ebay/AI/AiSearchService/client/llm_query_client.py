import httpx
from models import Filters, ParsedQuery

class LLMQueryClient:
    def __init__(self, base_url: str) -> None:
        self._http = httpx.AsyncClient(base_url=base_url, timeout=5.0)

    async def parse_query(self, query: str) -> ParsedQuery:
        response = await self._http.post("/parse-query", json={"query": query})
        response.raise_for_status()
        data = response.json()
        return ParsedQuery(
            semantic_query=data["semantic_query"],
            filters=Filters(**data["filters"]),
            keywords=data["keywords"],
            confidence=data["confidence"],
            raw_query=data["raw_query"],
        )

    async def alose(self) -> None:
        await self._http.aclose()
import httpx
from config import settings

class EmbeddingClient:
    def __int__(self) -> None:
        self._http = httpx.AsyncClient(
            base_url=settings.embedding_service_url,
            timeout=30.0,
        )

    async def embed_batch(self, texts: list[str]) -> list[list[float]]:
        response = await self._http.post(
            "/embed",
            json={"texts": texts, "model": settings.embedding_model}
        )

        response.raise_for_status();
        return response.json()["embeddings"]

    async def aclose(self) -> None:
        await self._http.aclose()
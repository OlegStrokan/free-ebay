import httpx


class OllamaClient:
    def __init__(self, base_url: str) -> None:
        self.http = httpx.AsyncClient(base_url=base_url, timeout=30.0)

    async def embed(self, text: str, model: str) -> list[float]:
        response = await self.http.post(
            "/api/embeddings",
            json={"model":model, "prompt": text}
        )
        response.raise_for_status()
        return response.json()["embedding"]

    async def aclose(self) -> None:
        await self.http.aclose()
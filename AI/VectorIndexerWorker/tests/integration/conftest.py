import httpx
import pytest
from testcontainers.core.container import DockerContainer


class _QdrantContainer(DockerContainer):
    def __init__(self) -> None:
        super().__init__("qdrant/qdrant:latest")
        self.with_exposed_ports(6333)

    def get_url(self) -> str:
        return f"http://localhost:{self.get_exposed_port(6333)}"


@pytest.fixture(scope="session")
def qdrant_url():
    with _QdrantContainer() as container:
        yield container.get_url()


# ---------------------------------------------------------------------------
# Qdrant REST API helpers — avoids pip/local module naming conflict
# ---------------------------------------------------------------------------

async def qdrant_create_collection(url: str, collection: str, size: int = 4) -> None:
    async with httpx.AsyncClient() as client:
        await client.put(
            f"{url}/collections/{collection}",
            json={"vectors": {"size": size, "distance": "Cosine"}},
        )


async def qdrant_delete_collection(url: str, collection: str) -> None:
    async with httpx.AsyncClient() as client:
        await client.delete(f"{url}/collections/{collection}")


async def qdrant_get_point(url: str, collection: str, point_id: str) -> dict | None:
    async with httpx.AsyncClient() as client:
        response = await client.post(
            f"{url}/collections/{collection}/points",
            json={"ids": [point_id], "with_payload": True},
        )
        results = response.json().get("result", [])
        return results[0] if results else None


async def qdrant_collection_exists(url: str, collection: str) -> bool:
    async with httpx.AsyncClient() as client:
        response = await client.get(f"{url}/collections")
        names = [c["name"] for c in response.json()["result"]["collections"]]
        return collection in names

import pytest
from elasticsearch import AsyncElasticsearch
from testcontainers.core.container import DockerContainer
from testcontainers.elasticsearch import ElasticSearchContainer
from qdrant_client import AsyncQdrantClient
from qdrant_client.models import Distance, PointStruct, VectorParams


# ---------------------------------------------------------------------------
# Elasticsearch container — security disabled for tests
# ---------------------------------------------------------------------------

@pytest.fixture(scope="session")
def es_url():
    container = ElasticSearchContainer("docker.elastic.co/elasticsearch/elasticsearch:8.13.0")
    container.with_env("xpack.security.enabled", "false")
    container.with_env("discovery.type", "single-node")
    with container:
        yield container.get_url()


# ---------------------------------------------------------------------------
# Qdrant container
# ---------------------------------------------------------------------------

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
# Seed helpers
# ---------------------------------------------------------------------------

VECTOR_SIZE = 4  # small vectors for speed


async def seed_es_index(url: str, index: str, docs: list[dict]) -> None:
    async with AsyncElasticsearch(url) as es:
        if not await es.indices.exists(index=index):
            await es.indices.create(index=index)
        for doc in docs:
            await es.index(index=index, id=doc["id"], document=doc)
        await es.indices.refresh(index=index)


async def drop_es_index(url: str, index: str) -> None:
    async with AsyncElasticsearch(url) as es:
        if await es.indices.exists(index=index):
            await es.indices.delete(index=index)


async def seed_qdrant_collection(url: str, collection: str, points: list[PointStruct]) -> None:
    async with AsyncQdrantClient(url=url) as client:
        collections = await client.get_collections()
        names = [c.name for c in collections.collections]
        if collection not in names:
            await client.create_collection(
                collection_name=collection,
                vectors_config=VectorParams(size=VECTOR_SIZE, distance=Distance.COSINE),
            )
        await client.upsert(collection_name=collection, points=points)


async def drop_qdrant_collection(url: str, collection: str) -> None:
    async with AsyncQdrantClient(url=url) as client:
        collections = await client.get_collections()
        names = [c.name for c in collections.collections]
        if collection in names:
            await client.delete_collection(collection_name=collection)

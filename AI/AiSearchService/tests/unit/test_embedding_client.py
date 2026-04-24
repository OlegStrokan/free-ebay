import json

import httpx
import pytest
import respx

from clients.embedding_client import EmbeddingClient


@pytest.fixture
def client() -> EmbeddingClient:
    return EmbeddingClient(base_url="http://embedding-test")


async def test_embed_posts_to_correct_endpoint_with_texts_list(client: EmbeddingClient) -> None:
    with respx.mock(base_url="http://embedding-test") as mock:
        route = mock.post("/embed").mock(
            return_value=httpx.Response(200, json={"embeddings": [[0.1, 0.2, 0.3]]})
        )
        await client.embed("hello")

    body = json.loads(route.calls[0].request.content)
    assert body == {"texts": ["hello"]}


async def test_embed_returns_first_embedding(client: EmbeddingClient) -> None:
    with respx.mock(base_url="http://embedding-test") as mock:
        mock.post("/embed").mock(
            return_value=httpx.Response(200, json={"embeddings": [[1.0, 2.0], [3.0, 4.0]]})
        )
        result = await client.embed("text")

    assert result == [1.0, 2.0]


async def test_embed_raises_on_http_error(client: EmbeddingClient) -> None:
    with respx.mock(base_url="http://embedding-test") as mock:
        mock.post("/embed").mock(return_value=httpx.Response(500))
        with pytest.raises(httpx.HTTPStatusError):
            await client.embed("text")

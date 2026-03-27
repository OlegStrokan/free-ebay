import json

import httpx
import pytest
import respx

from clients.ollama_client import OllamaClient


@pytest.fixture
def client() -> OllamaClient:
    return OllamaClient(base_url="http://ollama-test")


async def test_embed_sends_correct_json_body(client: OllamaClient) -> None:
    with respx.mock(base_url="http://ollama-test") as mock:
        route = mock.post("/api/embeddings").mock(
            return_value=httpx.Response(200, json={"embedding": [0.1, 0.2, 0.3]})
        )
        await client.embed("hello world", model="nomic-embed-text")

    assert route.called
    body = json.loads(route.calls[0].request.content)
    assert body == {"model": "nomic-embed-text", "prompt": "hello world"}


async def test_embed_returns_embedding_field(client: OllamaClient) -> None:
    with respx.mock(base_url="http://ollama-test") as mock:
        mock.post("/api/embeddings").mock(
            return_value=httpx.Response(200, json={"embedding": [1.0, 2.0, 3.0]})
        )
        result = await client.embed("text", model="nomic-embed-text")

    assert result == [1.0, 2.0, 3.0]


async def test_embed_raises_http_status_error_on_5xx(client: OllamaClient) -> None:
    with respx.mock(base_url="http://ollama-test") as mock:
        mock.post("/api/embeddings").mock(return_value=httpx.Response(500))
        with pytest.raises(httpx.HTTPStatusError):
            await client.embed("text", model="nomic-embed-text")


async def test_embed_raises_http_status_error_on_4xx(client: OllamaClient) -> None:
    with respx.mock(base_url="http://ollama-test") as mock:
        mock.post("/api/embeddings").mock(
            return_value=httpx.Response(400, json={"error": "bad request"})
        )
        with pytest.raises(httpx.HTTPStatusError):
            await client.embed("text", model="nomic-embed-text")


async def test_aclose_does_not_raise(client: OllamaClient) -> None:
    await client.aclose()

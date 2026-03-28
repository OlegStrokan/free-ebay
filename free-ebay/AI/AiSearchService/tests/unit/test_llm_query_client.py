import json

import httpx
import pytest
import respx

from clients.llm_query_client import LLMQueryClient, _fallback
from models import Filters, ParsedQuery

_PARSED_PAYLOAD = {
    "semantic_query": "keyboard",
    "filters": {
        "price_max": 50.0, "price_min": None, "color": None,
        "category": None, "brand": None,
        "attributes_required": [], "attributes_excluded": [],
    },
    "keywords": ["keyboard"],
    "confidence": 0.9,
}

_OLLAMA_RESPONSE = {"response": json.dumps(_PARSED_PAYLOAD)}


@pytest.fixture
def client() -> LLMQueryClient:
    return LLMQueryClient(base_url="http://ollama-test", model="phi3:mini")


async def test_parse_query_posts_to_ollama_generate(client: LLMQueryClient) -> None:
    with respx.mock(base_url="http://ollama-test") as mock:
        route = mock.post("/api/generate").mock(
            return_value=httpx.Response(200, json=_OLLAMA_RESPONSE)
        )
        await client.parse_query("red keyboard under 50")

    body = json.loads(route.calls[0].request.content)
    assert body["model"] == "phi3:mini"
    assert body["format"] == "json"
    assert body["stream"] is False
    assert "red keyboard under 50" in body["prompt"]


async def test_parse_query_deserializes_ollama_response(client: LLMQueryClient) -> None:
    with respx.mock(base_url="http://ollama-test") as mock:
        mock.post("/api/generate").mock(return_value=httpx.Response(200, json=_OLLAMA_RESPONSE))
        result = await client.parse_query("red keyboard under 50")

    assert isinstance(result, ParsedQuery)
    assert result.semantic_query == "keyboard"
    assert result.confidence == 0.9
    assert isinstance(result.filters, Filters)
    assert result.filters.price_max == 50.0
    assert result.raw_query == "red keyboard under 50"


async def test_parse_query_returns_fallback_on_ollama_error(client: LLMQueryClient) -> None:
    with respx.mock(base_url="http://ollama-test") as mock:
        mock.post("/api/generate").mock(return_value=httpx.Response(500))
        result = await client.parse_query("red keyboard")

    assert result.confidence == 0.0
    assert result.semantic_query == "red keyboard"
    assert result.raw_query == "red keyboard"


async def test_parse_query_returns_fallback_on_malformed_json(client: LLMQueryClient) -> None:
    bad_response = {"response": "not valid json {{{"}
    with respx.mock(base_url="http://ollama-test") as mock:
        mock.post("/api/generate").mock(return_value=httpx.Response(200, json=bad_response))
        result = await client.parse_query("red keyboard")

    assert result.confidence == 0.0


def test_fallback_splits_query_into_keywords() -> None:
    result = _fallback("red compact keyboard")
    assert result.keywords == ["red", "compact", "keyboard"]
    assert result.semantic_query == "red compact keyboard"
    assert result.confidence == 0.0


async def test_aclose_closes_http_client() -> None:
    client = LLMQueryClient(base_url="http://ollama-test", model="phi3:mini")
    await client.aclose()

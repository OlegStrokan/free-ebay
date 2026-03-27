import httpx
import pytest
import respx
from asgi_lifespan import LifespanManager

from main import app

FAKE_EMBEDDING = [0.5, 0.6, 0.7]


@pytest.fixture
async def ollama_mock():
    with respx.mock(base_url="http://localhost:11434", assert_all_called=False) as mock:
        mock.post("/api/embeddings").mock(
            return_value=httpx.Response(200, json={"embedding": FAKE_EMBEDDING})
        )
        yield mock


@pytest.fixture
async def client(ollama_mock):
    async with LifespanManager(app) as manager:
        async with httpx.AsyncClient(
            transport=httpx.ASGITransport(app=manager.app), base_url="http://test"
        ) as ac:
            yield ac

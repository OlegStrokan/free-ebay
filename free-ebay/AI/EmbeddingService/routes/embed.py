
from fastapi import APIRouter, Depends, HTTPException

from clients.ollama_client import OllamaClient
from config import settings
from models import EmbedResponse, EmbedRequest

router = APIRouter()

def get_ollama_client() -> OllamaClient:
    # will be overridden by app state in main.py?
    # @think: am i stupid or this is how this retarted language works?
    raise NotImplementedError

@router.post("/embed", response_model=EmbedResponse)
async def embed(
        request: EmbedRequest,
        client: OllamaClient = Depends(get_ollama_client),
) -> EmbedResponse:
    if not request.texts:
        raise HTTPException(status_code=400, detail="texts must not be empty")
    model = request.model or settings.default_model
    embeddings: list[list[float]] = []
    for text in request.texts:
        vector = await client.embed(text, model=model)
        embeddings.append(vector)
    return EmbedResponse(
            embeddings=embeddings,
            model=model,
            dimensions=len(embeddings[0]),
        )
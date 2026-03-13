from fastapi import APIRouter, Depends
from clients.ollama_client import OllamaClient
from models import ParseQueryRequest, ParseQueryResponse

router = APIRouter()

def get_ollama_client() -> OllamaClient:
    raise NotImplementedError

@router.post("/parse-query", response_model=ParseQueryResponse)
async def parse_query(
        request: ParseQueryRequest,
        client: OllamaClient = Depends(get_ollama_client),
) -> ParseQueryResponse:
    result = await client.parse_query(request.query)
    return ParseQueryResponse(
        semantic_query=result.semantic_query,
        filters=result.filters,
        keywords=result.keywords,
        confidence=result.confidence,
        raw_query=result.raw_query
    )
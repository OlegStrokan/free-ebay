from pydantic import BaseModel, Field

class EmbedRequest(BaseModel):
    texts: list[str] = Field(min_length=1)
    model: str | None = None

class EmbedResponse(BaseModel):
    embeddings: list[list[float]]
    model: str
    dimensions: int
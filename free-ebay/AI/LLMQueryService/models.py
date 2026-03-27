from pydantic import BaseModel, Field
class Filters(BaseModel):
    price_max: float | None = None
    price_min: float | None = None
    color: str | None = None
    category: str | None = None
    brand: str | None = None
    attributes_required: list[str] = Field(default_factory=list)
    attributes_excluded: list[str] = Field(default_factory=list)

class ParsedQuery(BaseModel):
    semantic_query: str
    filters: Filters = Field(default_factory=Filters)
    keywords: list[str] = Field(default_factory=list)
    confidence: float = 0.0
    raw_query: str = ""

class ParseQueryRequest(BaseModel):
    query: str

class ParseQueryResponse(BaseModel):
    semantic_query: str
    filters: Filters
    keywords: list[str]
    confidence: float
    raw_query: str
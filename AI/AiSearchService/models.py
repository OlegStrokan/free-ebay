from dataclasses import dataclass, field
from pydantic import BaseModel

# shared internal models

@dataclass
class ScoredResult:
    product_id: str
    score: float

@dataclass
class Filters:
    price_max: float | None = None
    price_min: float | None = None
    color: str | None = None
    category: str | None = None
    brand: str | None = None
    condition: str | None = None  # "New", "Used", "Refurbished"
    attributes_required: list[str] = field(default_factory=list)
    attributes_excluded: list[str] = field(default_factory=list)

@dataclass
class ParsedQuery:
    semantic_query:str
    filters: Filters
    keywords: list[str]
    confidence: float
    raw_query: str

@dataclass
class SearchResultItem:
    product_id: str
    name: str
    category: str
    price: float
    currency: str
    relevance_score: float
    image_urls: list[str]

@dataclass
class SearchPipelineResult:
    items: list[SearchResultItem]
    total: int
    parsed_query: ParsedQuery | None
    used_ai: bool

# http route models

class SearchRequest(BaseModel):
    query: str
    page: int = 1
    page_size: int = 20
    debug: bool = False

class SearchResponse(BaseModel):
    items: list[dict]
    total_count: int
    parsed_query_debug: str | None
    used_ai: bool
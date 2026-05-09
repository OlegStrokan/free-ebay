from pydantic import BaseModel
from enum import Enum


class EventType(str, Enum):
    PRODUCT_VIEWED = "ProductViewed"
    PRODUCT_CLICKED = "ProductClicked"
    PURCHASE_COMPLETED = "PurchaseCompleted"
    SEARCH_BOUNCED = "SearchBounced"


class ProductViewedEvent(BaseModel):
    user_id: str
    catalog_item_id: str
    duration_ms: int = 0
    source: str = "direct"  # search | recommendation | direct
    category: str | None = None
    brand: str | None = None
    price: float | None = None
    condition: str | None = None


class ProductClickedEvent(BaseModel):
    user_id: str
    catalog_item_id: str
    query_text: str
    rank: int = 0
    category: str | None = None
    brand: str | None = None
    price: float | None = None
    condition: str | None = None


class PurchaseCompletedEvent(BaseModel):
    user_id: str
    catalog_item_id: str
    listing_id: str | None = None
    price: float | None = None
    category: str | None = None
    brand: str | None = None
    condition: str | None = None


class SearchBouncedEvent(BaseModel):
    user_id: str
    query_text: str


class UserPreferenceProfile(BaseModel):
    user_id: str
    # weighted category affinity: {category: weight}
    categories: dict[str, float] = {}
    # weighted brand affinity: {brand: weight}
    brands: dict[str, float] = {}
    # price range (p25, p75 of viewed/purchased items)
    price_p25: float | None = None
    price_p75: float | None = None
    # condition preference (skew toward New vs Used)
    condition_weights: dict[str, float] = {}
    # total interactions counted
    interaction_count: int = 0

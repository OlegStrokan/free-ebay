from pydantic import BaseModel


class ProductAttribute(BaseModel):
    key: str
    value: str

class ProductEvent(BaseModel):
    product_id: str
    name: str
    description: str | None = None
    category: str | None = None
    price: float = 0.0
    currency: str = "USD"
    stock_quantity: int = 0
    image_urls: list[str] = []
    attributes: list[ProductAttribute] = []


class ProductStockUpdatedEvent(BaseModel):
    product_id: str
    previous_quantity: int
    new_quantity: int


class CatalogItemIdPayload(BaseModel):
    Value: str

class CategoryIdPayload(BaseModel):
    Value: str

class CatalogItemEvent(BaseModel):
    CatalogItemId: CatalogItemIdPayload
    Name: str
    Description: str | None = None
    CategoryId: CategoryIdPayload
    Gtin: str | None = None
    Attributes: list[ProductAttribute] = []
    ImageUrls: list[str] = []

class CatalogItemListingSummaryEvent(BaseModel):
    CatalogItemId: CatalogItemIdPayload
    MinPrice: float = 0.0
    MinPriceCurrency: str = "USD"
    SellerCount: int = 0
    HasActiveListings: bool = False
    BestCondition: str | None = None
    TotalStock: int = 0
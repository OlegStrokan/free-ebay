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
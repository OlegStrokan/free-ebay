from qdrant_client import AsyncQdrantClient
from qdrant_client.models import (
    Filter,
    FieldCondition,
    MatchValue,
    MatchExcept,
    Range,
)
from models import Filters, ScoredResult

# Domain rule: maps excluded attribute names to (field_key, excluded_values).
# Extend this map when new product attributes need to be filterable.
_ATTRIBUTE_EXCLUSION_MAP: dict[str, tuple[str, list[str]]] = {
    "numpad": ("layout", ["fullsize"]),  # "no numpad" → exclude fullsize layout keyboards
}

class QdrantSearchClient:
    def __init__(self, url: str, collection: str) -> None:
        self._client = AsyncQdrantClient(url=url)
        self._collection = collection

    async def search(
            self,
            vector: list[float],
            filters: Filters,
            top_k: int,
    ) -> list[ScoredResult]:
        must_conditions = [
            FieldCondition(key="status", match=MatchValue(value="active")),
            FieldCondition(key="has_active_listings", match=MatchValue(value=True)),
        ]

        if filters.price_max is not None:
            must_conditions.append(
                FieldCondition(key="min_price", range=Range(lte=filters.price_max))
            )

        if filters.price_min is not None:
            must_conditions.append(
                FieldCondition(key="max_price", range=Range(gte=filters.price_min))
            )

        if filters.color:
            must_conditions.append(
                FieldCondition(key="color", match=MatchValue(value=filters.color.lower()))
            )

        if filters.condition:
            must_conditions.append(
                FieldCondition(key="best_condition", match=MatchValue(value=filters.condition))
            )

        for excluded in filters.attributes_excluded:
            if excluded in _ATTRIBUTE_EXCLUSION_MAP:
                field_key, excluded_values = _ATTRIBUTE_EXCLUSION_MAP[excluded]
                must_conditions.append(
                    FieldCondition(key=field_key, match=MatchExcept(except_=excluded_values))
                )

        results = await self._client.search(
            collection_name=self._collection,
            query_vector=vector,
            query_filter=Filter(must=must_conditions),
            limit=top_k,
            with_payload=True,
        )

        return [ScoredResult(product_id=str(r.id), score=r.score) for r in results]



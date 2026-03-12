from qdrant_client import AsyncQdrantClient
from qdrant_client.models import (
    Filter,
    FieldCondition,
    MatchValue,
    MatchExcept,
    Range,
)
from models import Filters, ScoredResult

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
            FieldCondition(key="status", match=MatchValue(value="active"))
        ]

        if filters.price_max is not None:
            must_conditions.append(
                FieldCondition(key="price", range=Range(lte=filters.price_max))
            )

        if filters.price_min is not None:
            must_conditions.append(
                FieldCondition(key="price", range=Range(gte=filters.price_min))
            )

        if filters.color:
            must_conditions.append(
                FieldCondition(key="color", match=MatchValue(value=filters.color.lower()))
            )

        for excluded in filters.attributes_excluded:
            if excluded == "numpad":
                must_conditions.append(
                    FieldCondition(key="layout", match=MatchExcept(except_=["fullsize"]))
                )

        results = await self._client.search(
            collection_name=self._collection,
            query_vector=vector,
            query_filter=Filter(must=must_conditions),
            limit=top_k,
            with_payload=True,
        )

        return [ScoredResult(product_id=str(r.id), score=r.score) for r in results]



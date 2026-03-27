from elasticsearch import AsyncElasticsearch
from models import ParsedQuery, ScoredResult

class ElasticsearchClient:
    def __init__(self, url: str, index: str) -> None:
        self._es = AsyncElasticsearch(url)
        self._index =index

    async def search(self, parsed: ParsedQuery, top_k: int) -> list[ScoredResult]:
        query: dict = {
            "bool": {
                "must": [
                    {
                        "multi_match": {
                            "query": "".join(parsed.keywords),
                            "fields": ["name^3", "description", "category^2"],
                            "type": "best_fields",
                        }
                    }
                ],
                "filter": [],
            }
        }

        if parsed.filters.price_max:
            query["bool"]["filter"].append({ "range": { "price": { "lte": parsed.filters.price_max}}})

        if parsed.filters.price_min:
            query["bool"]["filter"].append({"range": {"price": {"gte": parsed.filters.price_min}}})

        result = await self._es.search(index=self._index, query=query, size=top_k)
        return [
            ScoredResult(product_id=h["_source"]["id"], score=h["_score"])
            for h in result["hits"]["hits"]
        ]

    async def aclose(self) -> None:
        await self._es.close()
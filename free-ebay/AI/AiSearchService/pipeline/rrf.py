from models import ScoredResult


# RPF algorithm, k=60 just default value
def rrf_merge(
        list_a: list[ScoredResult],
        list_b: list[ScoredResult],
        k: int = 60,
) -> list[ScoredResult]:
    scores: dict[str, float] = {}

    for rank, result in enumerate(list_a, start=1):
        scores[result.product_id] = scores.get(result.product_id, 0.0) + 1.0 / (k + rank)

    for rank, result in enumerate(list_b, start=1):
        scores[result.product_id] = scores.get(result.product_id, 0.0) + 1.0 / (k + rank)

    merged = sorted(scores.items(), key=lambda x: x[1], reverse=True)
    return [ScoredResult(product_id=pid, score=score) for pid, score in merged]
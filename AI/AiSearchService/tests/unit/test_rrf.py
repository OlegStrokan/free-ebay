import pytest

from models import ScoredResult
from pipeline.rrf import rrf_merge


def _r(pid: str, score: float = 0.0) -> ScoredResult:
    return ScoredResult(product_id=pid, score=score)


def test_rrf_scores_are_reciprocal_rank_sum() -> None:
    a = [_r("p1"), _r("p2")]
    b = [_r("p3"), _r("p1")]
    result = rrf_merge(a, b, k=60)

    scores = {r.product_id: r.score for r in result}
    # p1 appears rank-1 in a and rank-2 in b
    assert scores["p1"] == pytest.approx(1 / 61 + 1 / 62)
    # p2 appears only rank-2 in a
    assert scores["p2"] == pytest.approx(1 / 62)
    # p3 appears only rank-1 in b
    assert scores["p3"] == pytest.approx(1 / 61)


def test_rrf_result_is_sorted_descending() -> None:
    a = [_r("p1"), _r("p2"), _r("p3")]
    b = [_r("p3"), _r("p2"), _r("p1")]
    result = rrf_merge(a, b, k=60)

    scores = [r.score for r in result]
    assert scores == sorted(scores, reverse=True)


def test_rrf_disjoint_lists_contain_all_ids() -> None:
    a = [_r("a"), _r("b")]
    b = [_r("c"), _r("d")]
    result = rrf_merge(a, b, k=60)
    ids = {r.product_id for r in result}
    assert ids == {"a", "b", "c", "d"}


def test_rrf_empty_lists_return_empty() -> None:
    assert rrf_merge([], [], k=60) == []


def test_rrf_one_empty_list_returns_other_rescored() -> None:
    a = [_r("p1"), _r("p2")]
    result = rrf_merge(a, [], k=60)
    assert len(result) == 2
    ids = [r.product_id for r in result]
    assert ids == ["p1", "p2"]

import pytest
import fakeredis.aioredis

from cooccurrence import CoOccurrenceTracker


@pytest.fixture
async def redis_client():
    client = fakeredis.aioredis.FakeRedis(decode_responses=True)
    yield client
    await client.aclose()


@pytest.fixture
def tracker(redis_client) -> CoOccurrenceTracker:
    return CoOccurrenceTracker(redis_client=redis_client)


async def test_first_purchase_creates_no_cooccurrence(tracker, redis_client):
    """First purchase for a user has no history to pair with."""
    await tracker.record_purchase("user-1", "item-A")

    results = await tracker.get_co_occurrences("item-A")
    assert results == []


async def test_second_purchase_creates_bidirectional_cooccurrence(tracker, redis_client):
    """Two purchases by the same user create bidirectional co-occurrence."""
    await tracker.record_purchase("user-1", "item-A")
    await tracker.record_purchase("user-1", "item-B")

    results_a = await tracker.get_co_occurrences("item-A")
    results_b = await tracker.get_co_occurrences("item-B")

    assert len(results_a) == 1
    assert results_a[0] == ("item-B", 1.0)

    assert len(results_b) == 1
    assert results_b[0] == ("item-A", 1.0)


async def test_repeated_purchases_increment_score(tracker, redis_client):
    """Multiple users buying the same pair increases the co-occurrence score."""
    # User 1 buys A then B
    await tracker.record_purchase("user-1", "item-A")
    await tracker.record_purchase("user-1", "item-B")

    # User 2 also buys A then B
    await tracker.record_purchase("user-2", "item-A")
    await tracker.record_purchase("user-2", "item-B")

    results = await tracker.get_co_occurrences("item-A")
    assert len(results) == 1
    assert results[0] == ("item-B", 2.0)


async def test_multiple_items_sorted_by_score(tracker, redis_client):
    """Co-occurrences are returned sorted by score descending."""
    # User buys A, then B, then C
    await tracker.record_purchase("user-1", "item-A")
    await tracker.record_purchase("user-1", "item-B")
    await tracker.record_purchase("user-1", "item-C")

    # Another user also buys A then C (but not B)
    await tracker.record_purchase("user-2", "item-A")
    await tracker.record_purchase("user-2", "item-C")

    results = await tracker.get_co_occurrences("item-A")
    # C should have higher score (paired with A twice) than B (once)
    assert results[0][0] == "item-C"
    assert results[0][1] == 2.0
    assert results[1][0] == "item-B"
    assert results[1][1] == 1.0


async def test_limit_parameter_caps_results(tracker, redis_client):
    """get_co_occurrences respects the limit parameter."""
    await tracker.record_purchase("user-1", "item-A")
    for i in range(5):
        await tracker.record_purchase("user-1", f"item-{i}")

    results = await tracker.get_co_occurrences("item-A", limit=2)
    assert len(results) == 2


async def test_self_purchase_does_not_create_self_cooccurrence(tracker, redis_client):
    """Buying the same item twice doesn't create a self-referencing co-occurrence."""
    await tracker.record_purchase("user-1", "item-A")
    await tracker.record_purchase("user-1", "item-A")

    results = await tracker.get_co_occurrences("item-A")
    assert all(item_id != "item-A" for item_id, _ in results)


async def test_different_users_contribute_independently(tracker, redis_client):
    """Purchases from different users each contribute to co-occurrence counts."""
    # 3 different users all buy A then B
    for i in range(3):
        await tracker.record_purchase(f"user-{i}", "item-A")
        await tracker.record_purchase(f"user-{i}", "item-B")

    results = await tracker.get_co_occurrences("item-A")
    assert results[0] == ("item-B", 3.0)


async def test_purchase_history_stored_per_user(tracker, redis_client):
    """Each user's purchase history is independent."""
    await tracker.record_purchase("user-1", "item-A")
    await tracker.record_purchase("user-2", "item-B")

    # No co-occurrence since different users
    results_a = await tracker.get_co_occurrences("item-A")
    results_b = await tracker.get_co_occurrences("item-B")
    assert results_a == []
    assert results_b == []


async def test_recent_purchases_key_has_ttl(tracker, redis_client):
    """The recent purchases list should have a TTL set."""
    await tracker.record_purchase("user-1", "item-A")

    ttl = await redis_client.ttl("user:user-1:recent_purchases")
    assert ttl > 0


async def test_cooccurrence_key_has_ttl(tracker, redis_client):
    """The co-occurrence sorted set keys should have a TTL set."""
    await tracker.record_purchase("user-1", "item-A")
    await tracker.record_purchase("user-1", "item-B")

    ttl_a = await redis_client.ttl("cooccurrence:purchase:item-A")
    ttl_b = await redis_client.ttl("cooccurrence:purchase:item-B")
    assert ttl_a > 0
    assert ttl_b > 0


async def test_purchase_history_trimmed_to_max(tracker, redis_client):
    """Purchase history list is capped at 50 items."""
    for i in range(55):
        await tracker.record_purchase("user-1", f"item-{i}")

    length = await redis_client.llen("user:user-1:recent_purchases")
    assert length == 50


async def test_get_co_occurrences_for_unknown_item_returns_empty(tracker):
    """Querying a never-seen item returns an empty list."""
    results = await tracker.get_co_occurrences("never-purchased-item")
    assert results == []

# VectorIndexerWorker — Notes

---

## 1. Bug: ProductStockUpdatedEvent is silently dropped

### What's happening

`consumer.py` handles three event types:

```python
match event_type:
    case "ProductCreateEvent" | "ProductUpdatedEvent":
        await indexer.upsert(payload)
    case "ProductDeletedEvent":
        await indexer.delete(payload["product_id"])
    case _:
        log.warning("unknown_event_type", event_type=event_type)  # ← ProductStockUpdatedEvent ends up here
```

`ProductStockUpdatedEvent` is a **separate event** from `ProductUpdatedEvent`. It's raised by
`Product.UpdateStock()` and `Product.AdjustStock()` in the Product domain and published to Kafka.
The worker never handles it, so the Qdrant vector index never learns about stock changes.

### Consequence

- Product sells out → `AdjustStock(-1)` → `ProductStockUpdatedEvent` → Qdrant still has `status: active`
- Product restocked → `UpdateStock(50)` → `ProductStockUpdatedEvent` → Qdrant still has `status: out_of_stock`

Buyers get semantically correct ranking but wrong availability in the payload. AI search can return
out-of-stock products ranked highly with no indication they're unavailable.

### Why it's high volume

Every completed order triggers `AdjustStock` in the Inventory service → Product publishes
`ProductStockUpdatedEvent`. At 10k orders/day that's at minimum 10k stock events per day, spiking
around peak hours. This is the real high-frequency Kafka event, not product creates.

### Why you can't just call `indexer.upsert(payload)`

`ProductStockUpdatedEvent` only carries `ProductId`, `PreviousQuantity`, `NewQuantity`, `OccurredOn`.
It does not carry `name`, `description`, `category`, `attributes`, or `image_urls`. Calling
`indexer.upsert` blindly would wipe those fields with empty/default values.

### How to fix it

You need a **partial Qdrant payload update** — update only the `status` and `stock_quantity` fields
without touching the vector or the rest of the payload.

**Step 1 — add `ProductStockUpdatedEvent` model to `models.py`:**

```python
class ProductStockUpdatedEvent(BaseModel):
    product_id: str
    previous_quantity: int
    new_quantity: int
```

**Step 2 — add `update_stock` to `Indexer`:**

```python
async def update_stock(self, raw: dict) -> None:
    event = ProductStockUpdatedEvent.model_validate(raw)
    status = "active" if event.new_quantity > 0 else "out_of_stock"
    await self.qdrant.update_payload(
        product_id=event.product_id,
        patch={"stock_quantity": event.new_quantity, "status": status},
    )
    log.info("product_stock_updated", product_id=event.product_id, status=status)
```

**Step 3 — add `update_payload` to `QdrantIndexClient`:**

Qdrant has a `set_payload` API that patches specific fields without touching the vector:

```python
async def update_payload(self, product_id: str, patch: dict) -> None:
    self._client.set_payload(
        collection_name=self._collection,
        payload=patch,
        points=Filter(
            must=[FieldCondition(key="product_id", match=MatchValue(value=product_id))]
        ),
    )
```

**Step 4 — handle the event in `consumer.py`:**

```python
match event_type:
    case "ProductCreateEvent" | "ProductUpdatedEvent":
        await indexer.upsert(payload)
    case "ProductStockUpdatedEvent":
        await indexer.update_stock(payload)
    case "ProductDeletedEvent":
        await indexer.delete(payload["product_id"])
    case _:
        log.warning("unknown_event_type", event_type=event_type)
```

---

## 2. Missing product type: catalog SKU vs unique listing

### Current state

The `Product` aggregate owns every field directly: `name`, `description`, `price`, `stock_quantity`,
`sellerId`, `attributes`. Every product is a fully independent entity. There is no concept of two
sellers sharing the same product definition.

```
Product (current)
  ├── SellerId        ← who owns it
  ├── Name
  ├── Description
  ├── Price           ← per-seller price
  ├── StockQuantity   ← per-seller stock
  └── Attributes
```

This is correct for **eBay-style**: a used Sony camera listed by Seller A is not the same thing as
a used Sony camera listed by Seller B. They have different conditions, prices, photos.

It does not support **Amazon-style**: one shared "Sony A7 IV Body" product page where 12 sellers
each offer their price and stock, and buyers compare them on a single page.

### This is not a bug — it's a missing feature

Nothing is broken today. If your market is second-hand/unique goods (eBay mode), the current model
is correct and complete. You only need the change below if you intend to support catalog-based
selling where multiple sellers compete on the same SKU.

### How to implement Amazon-style catalog

Introduce two separate aggregates: a shared `CatalogItem` (the "what is this thing") and per-seller
`Listing` (the "how much and how many").

```
CatalogItem
  ├── CatalogItemId
  ├── Name
  ├── Description
  ├── CategoryId
  ├── Attributes       ← canonical spec (brand, dimensions, etc.)
  └── ImageUrls        ← canonical product photos

Listing
  ├── ListingId
  ├── CatalogItemId    ← FK to shared definition
  ├── SellerId
  ├── Price            ← per-seller
  ├── StockQuantity    ← per-seller
  ├── Condition        ← new / used / refurbished
  └── SellerNotes      ← "minor scratch on bottom"
```

**Domain rules:**
- Only one `CatalogItem` per real-world product (deduplication by GTIN/EAN/UPC or manual review)
- Multiple `Listing`s per `CatalogItem`, one per seller
- `CatalogItem` raises `CatalogItemCreatedEvent` / `CatalogItemUpdatedEvent` — consumed by
  VectorIndexerWorker and Catalog service for the shared product page
- `Listing` raises `ListingCreatedEvent` / `ListingPriceChangedEvent` / `ListingStockChangedEvent` —
  consumed for per-seller inventory and the price comparison block on the product page

**What changes in the vector index:**

The embedding is over `CatalogItem` fields (name, description, category, canonical attributes) — one
vector per real-world product. The Qdrant payload includes an aggregated `min_price` / `seller_count`
field updated when any `Listing` changes. This way search returns one result per product, not one
result per seller listing.

**Migration path from current model:**

The current `Product` entity maps 1:1 to the eBay-style listing. Migration would be:
1. Introduce `CatalogItem` and `Listing` as new aggregates in a new bounded context
2. Keep the existing `Product` aggregate for eBay-mode sellers unchanged
3. At the API (Gateway) level, route catalog-based product creation to the new flow
4. Run both models in parallel — no big-bang migration needed

---

## 3. Bidi streaming + multiplexing for AiSearchService — when to build it

### Current architecture

```
C# Search service  ──gRPC unary──►  Python AiSearchService
                                         │
                                    orchestrator.py
                                         ├── LLM parse (1.5s timeout)
                                         ├── asyncio.gather(embed_task, es_task)
                                         ├── qdrant_task (needs vector from embed)
                                         └── rrf_merge → SearchResponse
```

Python owns the merge. C# sends one request and waits for one response. This is correct for 2 legs.

### Why it doesn't pay off now

RRF requires all candidates before it can produce a ranking. With 2 legs both complete in under
100ms. The merge is instantaneous. There's no partial result that's useful before the other side
finishes. Streaming adds protocol complexity for zero gain.

### Why it pays off at 4 legs

With 4 legs at different speeds:

```
t=0ms:    C# sends 4 stream requests (corrId A, B, C, D)
t=50ms:   corrId B: ES keyword results arrive  → C# partial RRF: 50ms
t=80ms:   corrId A: Qdrant vector results arrive → C# updates RRF: 80ms
t=150ms:  corrId C: seller-score results arrive  → C# updates RRF: 150ms
t=200ms:  deadline hit → C# returns current RRF result to buyer
t=300ms:  corrId D: personalized reranking arrives → too late, ignored
```

With unary: you wait 300ms for all 4, or you implement `asyncio.wait_for` with fallback logic inside
Python. The timeout logic for "which legs are optional" lives in Python and is hard to evolve. With
streaming, C# owns the deadline and decides which results to include based on what arrived. Python
just fires tasks and streams back whatever completes.

The streaming boundary is between **C# Search service and Python AiSearchService**:

```
C# Search service  ──bidi stream──►  Python AiSearchService
     sends: {corrId:"A", type:VECTOR, query:"..."}   ──► Qdrant task (async)
     sends: {corrId:"B", type:KEYWORD, query:"..."}  ──► Elasticsearch task (async)
     sends: {corrId:"C", type:SELLER_SCORE, ids:[]}  ──► Seller scoring task (async)
     sends: {corrId:"D", type:PERSONALIZED, userId}  ──► Reranker task (async)
     
     receives: {corrId:"B", results}  ← ES done first
     receives: {corrId:"A", results}  ← Qdrant done
     receives: {corrId:"C", results}  ← seller score done
     deadline → merge and return; corrId D skipped
```

### Two search legs worth adding

**Leg 3: Seller reputation scoring**

Boost or penalize results by seller rating and fulfillment track record. A product from a 4.9-star
seller with 98% on-time delivery should rank above an identical product from a 2.1-star seller, even
if semantic similarity is equal. This is cheap to implement: Seller service exposes a gRPC endpoint
returning a score map `{sellerId → score}`, Python multiplies RRF scores by the reputation factor.
Typical latency: 20–40ms (Redis cache in front of Seller DB). Never blocks other legs.

**Leg 4: Personalized reranking**

Reorder results based on buyer history: categories they browse frequently, price range they buy in,
brands they repeatedly buy. This is the leg that is slow and cold: the model needs the user's
embedding vector (computed from purchase history), which may not be cached. Typical latency:
50–300ms depending on cache warmth. This is exactly the optional leg — if it doesn't arrive within
the C# deadline, you return the RRF+seller result which is still good. If it does arrive in time,
the ranking improves. The buyer never waits 300ms for personalization; they get a result in 200ms
and personalization is a bonus when it's fast.

### Recommendation

Don't build the bidi streaming protocol now. The unary call is correct for today's 2-leg pipeline.

The trigger to switch: when seller scoring or personalized reranking is added and you measure that
the slowest leg is meaningfully increasing p95 latency on searches. At that point the bidi stream
lets you implement graceful degradation (return without the slow leg) cleanly, with the budget
owned by C# not buried in Python timeout logic.

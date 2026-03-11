# Vector Indexer Worker

Consume  `product.*` events from kafka -> build corpus -> call embedding service -> upsert into Qdrant

- no http, no grpc. just long-running async kafka consumer that writes to Qdrant
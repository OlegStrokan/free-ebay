# Gabriela

Plan: 2 apps, one extremely complicated microservice zoo ebay with crypto support + public api, and second one is less complicated but also stupidly complex xiaoping-express

ebay stack:

Tech info (not complete)
user service

user service probably should be a rest api, but 90% of other microservices use grpc and don't expose any methods for public, all use gateway, so to make it consistent
i prefer consistency to logic. user-service will be grpc microservice, yes it's overkill, but zhizn' igra, igray krasivo


auth service

some auth stuff. simple as fuck

order service:
saga based transaction with sprinkle of outbox transactions, kafka workers, ddd aggregates, distributed locking, watchdogs, event sourcing and cqrs, and ALMOST exactly-once processing
also suppoting b2b orders and subscription (recurring order)

supraphysiological amount of lean unit tests, also some integration and e2e tests type shit


product service: 
internal kafka cqrs service. used by order (kafka) + REST APIcko for product-guy 


catalog service: 
consumes product events from kafka and write it to elasticsarc
search service and ai-search-service use this info from elastic

AI: 

ai-search-service: 
- receive request, start ai pipeline, make 2 parallel calls to ai and elastic, merge and return result
- directly call llm-query-service and embedding-service

vector-indexer-worker:
- consume product events from kafka and upsert into Qdrant 

embedding-service
- bridge between LLM and eshop microservices (what vector represents of what user means)

llm-query-service (should be moved to ai-search-service)
- parser for llm: transform user prompt into structured data (what user means)


search service:
search items in 2 ways:
- without AI => plain elasticsearch
- with AI => call pythonAI service, falls back to elastic on timeout/error



Catalog vs Inventory

- Catalog service: builds and updates search read model in Elasticsearch by consuming product events. It is a read/search projection service.
- Inventory service: owns real-time stock state and reservation lifecycle used by Order saga. It is a transactional write service.


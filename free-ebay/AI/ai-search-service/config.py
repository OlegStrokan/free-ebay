from pydantic_settings import BaseSettings, SettingsConfigDict

class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="AI_SEARCH_")

    embedding_service_url: str = "http://localhost:8001"
    llm_query_service_url: str = "http://localhost:8002"

    qdrant_url: str = "http://localhost:6333"
    qdrant_collection: str = "products"

    es_url: str = "http://localhost:9200"
    es_index: str = "products"

    gprc_port: int = 50051
    http_port: int = 8003

    llm_timeout_seconds: float = 1.5
    top_k: int = 50
    rrf_K: int = 60

settings = Settings()
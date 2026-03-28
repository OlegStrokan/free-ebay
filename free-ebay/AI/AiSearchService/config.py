from pydantic_settings import BaseSettings, SettingsConfigDict

class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="AI_SEARCH_")

    embedding_service_url: str = "http://localhost:8001"

    ollama_base_url: str = "http://localhost:11434"
    llm_model: str = "phi3:mini"
    llm_temperature: float = 0.1
    llm_num_predict: int = 256
    llm_ollama_timeout: float = 5.0

    qdrant_url: str = "http://localhost:6333"
    qdrant_collection: str = "products"

    es_url: str = "http://localhost:9200"
    es_index: str = "products"

    gprc_port: int = 50051
    http_port: int = 8003

    llm_timeout_seconds: float = 1.5
    top_k: int = 50
    rrf_K: int = 60

    @property
    def grpc_port(self) -> int:
        return self.gprc_port

    @property
    def rrf_k(self) -> int:
        return self.rrf_K

settings = Settings()
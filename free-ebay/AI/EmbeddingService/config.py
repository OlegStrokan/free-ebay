from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="EMPEDDING_")

    ollama_base_url: str = "http://localhost:11434"
    default_model: str = "nomic-embed-text"
    port: int = 8001

settings = Settings()
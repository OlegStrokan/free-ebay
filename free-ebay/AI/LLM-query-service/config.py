from pydantic_settings import BaseSettings, SettingsConfigDict

class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="LLM_")

    ollama_base_url: str = "http://localhost:11434"
    model: str = "phi3:mini"
    temperature: float = 0.1
    num_predict: int = 256
    timeout_seconds: float = 5.0
    port: int = 8002

settings = Settings()
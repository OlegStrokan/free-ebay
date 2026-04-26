import grpc
import grpc.aio
import structlog

import embedding_pb2
import embedding_pb2_grpc
from clients.ollama_client import OllamaClient

log = structlog.get_logger()


class EmbeddingServicer(embedding_pb2_grpc.EmbeddingServiceServicer):
    def __init__(self, ollama_client: OllamaClient, default_model: str) -> None:
        self._ollama = ollama_client
        self._default_model = default_model

    async def EmbedStream(
        self,
        request_iterator: grpc.aio.ServicerContext,
        context: grpc.aio.ServicerContext,
    ):
        async for request in request_iterator:
            model = request.model or self._default_model
            try:
                vector = await self._ollama.embed(request.text, model=model)
                yield embedding_pb2.EmbedStreamResponse(
                    correlation_id=request.correlation_id,
                    vector=vector,
                    dimensions=len(vector),
                )
                log.debug("embed_stream_ok", corr_id=request.correlation_id, dims=len(vector))
            except Exception as exc:
                log.error("embed_stream_error", corr_id=request.correlation_id, error=str(exc))
                await context.abort(grpc.StatusCode.INTERNAL, f"Ollama error: {exc}")
                return


def build_grpc_server(ollama_client: OllamaClient, default_model: str) -> grpc.aio.Server:
    server = grpc.aio.server(
        options=[
            # keep the stream alive during Kafka dry spells
            ("grpc.keepalive_time_ms", 60_000),
            ("grpc.keepalive_timeout_ms", 20_000),
            ("grpc.keepalive_permit_without_calls", True),
            ("grpc.http2.max_pings_without_data", 0),
        ]
    )
    embedding_pb2_grpc.add_EmbeddingServiceServicer_to_server(
        EmbeddingServicer(ollama_client, default_model),
        server,
    )
    return server

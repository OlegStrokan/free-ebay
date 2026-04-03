namespace Infrastructure.Elasticsearch;

public sealed class ElasticsearchIndexingException(string message) : Exception(message);
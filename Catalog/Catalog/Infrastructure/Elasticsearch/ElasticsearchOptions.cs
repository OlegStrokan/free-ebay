namespace Infrastructure.Elasticsearch;

public sealed class ElasticsearchOptions
{
    //@todo: use configuration variables
    public const string SectionName = "Elasticsearch";
    public string Url { get; set; } = "http://localhost:9200";
    public string IndexName { get; set; } = "products";
}

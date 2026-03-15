using System.Text.Json.Serialization;

namespace Infrastructure.ElasticSearch.Documents;

public sealed class ProductSearchDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("name")] 
    public string Name { get; set; } = default!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = default!;

    [JsonPropertyName("category")]
    public string Category { get; set; } = default!;
    
    [JsonPropertyName("price")]
    public double Price { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = default!;
    
    [JsonPropertyName("stock")]
    public int Stock { get; set; }
    
    [JsonPropertyName("color")]
    public string? Color { get; set; }
    
    [JsonPropertyName("layout")]
    public string? Layout { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("image_urls")] 
    public List<string> ImageUrls { get; set; } = [];
    
    [JsonPropertyName("indexed_at")]
    public DateTime IndexedAt { get; set; }
}
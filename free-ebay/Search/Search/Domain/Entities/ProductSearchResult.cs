using Domain.Exceptions;
using Domain.SearchResults.ValueObjects;

namespace Domain.Entities;

public sealed class ProductSearchResult
{
    private Guid _productId;
    private string _name;
    private string _category;
    private Money _price;
    private double _relevanceScore;
    private List<string> _imageUrls;

    public Guid ProductId => _productId;
    public string Name => _name;
    public string Category => _category;
    public Money Price => _price;
    public double RelevanceScore => _relevanceScore;
    public List<string> ImageUrls => _imageUrls;
    
    private ProductSearchResult() {}

    public static ProductSearchResult Create(
        Guid productId,
        string name,
        string category,
        Money price,
        RelevanceScore relevanceScore,
        List<string> imageUrls)
    {

        if (productId == Guid.Empty)
            throw new DomainException("ProductSearchResult: productId cannot be empty.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("ProductSearchResult: name cannot be empty.");

        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException("ProductSearchResult: category cannot be empty.");

        if (price is null)
            throw new DomainException("ProductSearchResult: price cannot be null.");

        if (relevanceScore < 0)
            throw new DomainException("ProductSearchResult: relevance score cannot be negative.");

        return new ProductSearchResult
        {
            _productId = productId,
            _name = name,
            _category = category,
            _price = price,
            _relevanceScore = relevanceScore,
            _imageUrls = imageUrls ?? []
        };


    }
}
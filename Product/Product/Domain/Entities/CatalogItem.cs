using Domain.Common;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class CatalogItem : AggregateRoot<CatalogItemId>
{
    private string _name = null!;
    private string _description = null!;
    private CategoryId _categoryId = null!;
    private string? _gtin;
    private List<ProductAttribute> _attributes = new();
    private List<string> _imageUrls = new();
    private DateTime _createdAt;
    private DateTime? _updatedAt;

    public string Name => _name;
    public string Description => _description;
    public CategoryId CategoryId => _categoryId;
    public string? Gtin => _gtin;
    public IReadOnlyList<ProductAttribute> Attributes => _attributes.AsReadOnly();
    public IReadOnlyList<string> ImageUrls => _imageUrls.AsReadOnly();
    public DateTime CreatedAt => _createdAt;
    public DateTime? UpdatedAt => _updatedAt;

    private CatalogItem() { }

    public static CatalogItem Create(
        string name,
        string description,
        CategoryId categoryId,
        string? gtin,
        List<ProductAttribute>? attributes,
        List<string>? imageUrls)
    {
        ValidateName(name);

        var createdAt = DateTime.UtcNow;
        var catalogItem = new CatalogItem
        {
            Id = CatalogItemId.CreateUnique(),
            _name = name.Trim(),
            _description = description,
            _categoryId = categoryId,
            _gtin = NormalizeGtin(gtin),
            _attributes = attributes ?? [],
            _imageUrls = imageUrls ?? [],
            _createdAt = createdAt
        };

        catalogItem.AddDomainEvent(new CatalogItemCreatedEvent(
            catalogItem.Id,
            catalogItem._name,
            catalogItem._description,
            catalogItem._categoryId,
            catalogItem._gtin,
            catalogItem._attributes,
            catalogItem._imageUrls,
            createdAt));

        return catalogItem;
    }

    public void Update(
        string name,
        string description,
        CategoryId categoryId,
        string? gtin,
        List<ProductAttribute>? attributes,
        List<string>? imageUrls)
    {
        ValidateName(name);

        _name = name.Trim();
        _description = description;
        _categoryId = categoryId;
        _gtin = NormalizeGtin(gtin);
        _attributes = attributes ?? [];
        _imageUrls = imageUrls ?? [];
        _updatedAt = DateTime.UtcNow;

        AddDomainEvent(new CatalogItemUpdatedEvent(
            Id,
            _name,
            _description,
            _categoryId,
            _gtin,
            _attributes,
            _imageUrls,
            _updatedAt.Value));
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Catalog item name cannot be empty");
    }

    private static string? NormalizeGtin(string? gtin)
        => string.IsNullOrWhiteSpace(gtin) ? null : gtin.Trim();
}
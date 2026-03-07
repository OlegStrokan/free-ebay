using Domain.Common;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Product : AggregateRoot<ProductId>
{
    private string         _name          = null!;
    private string         _description   = null!;
    private CategoryId     _categoryId    = null!;  // resolved to name by Application layer
    private Money          _price         = null!;
    private ProductStatus  _status        = null!;
    private SellerId       _sellerId      = null!;
    private List<ProductAttribute> _attributes = new();
    private List<string>   _imageUrls     = new();
    private int            _stockQuantity;
    private DateTime       _createdAt;
    private DateTime?      _updatedAt;

    public string        Name          => _name;
    public string        Description   => _description;
    public CategoryId    CategoryId    => _categoryId;
    public Money         Price         => _price;
    public ProductStatus Status        => _status;
    public SellerId      SellerId      => _sellerId;
    public int           StockQuantity => _stockQuantity;
    public DateTime      CreatedAt     => _createdAt;
    public DateTime?     UpdatedAt     => _updatedAt;
    public IReadOnlyList<ProductAttribute> Attributes => _attributes.AsReadOnly();
    public IReadOnlyList<string>           ImageUrls  => _imageUrls.AsReadOnly();

    private Product() { }

    // -------------------------------------------------------------------------
    // Commands — mutate state directly, then register an outbox event.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a new product in Draft status. The seller must explicitly call
    /// <see cref="Activate"/> to make it visible in search.
    /// </summary>
    public static Product Create(
        SellerId               sellerId,
        string                 name,
        string                 description,
        CategoryId             categoryId,
        Money                  price,
        int                    initialStock,
        List<ProductAttribute> attributes,
        List<string>           imageUrls)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty");

        if (initialStock < 0)
            throw new DomainException("Initial stock cannot be negative");

        var product = new Product
        {
            Id             = ProductId.CreateUnique(),
            _sellerId      = sellerId,
            _name          = name,
            _description   = description,
            _categoryId    = categoryId,
            _price         = price,
            _stockQuantity = initialStock,
            _attributes    = attributes ?? [],
            _imageUrls     = imageUrls ?? [],
            _status        = ProductStatus.Draft,
            _createdAt     = DateTime.UtcNow,
        };

        product.AddDomainEvent(new ProductCreatedEvent(
            ProductId:    product.Id,
            SellerId:     sellerId,
            Name:         name,
            Description:  description,
            CategoryId:   categoryId,
            Price:        price,
            InitialStock: initialStock,
            Attributes:   product._attributes,
            ImageUrls:    product._imageUrls,
            CreatedAt:    product._createdAt));

        return product;
    }

    /// <summary>Updates mutable product fields. Deleted products cannot be updated.</summary>
    public void Update(
        string                 name,
        string                 description,
        CategoryId             categoryId,
        Money                  price,
        List<ProductAttribute> attributes,
        List<string>           imageUrls)
    {
        if (_status == ProductStatus.Deleted)
            throw new InvalidProductOperationException(Id.Value, "Cannot update a deleted product");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty");

        _name        = name;
        _description = description;
        _categoryId  = categoryId;
        _price       = price;
        _attributes  = attributes ?? [];
        _imageUrls   = imageUrls ?? [];
        _updatedAt   = DateTime.UtcNow;

        AddDomainEvent(new ProductUpdatedEvent(
            ProductId:   Id,
            Name:        _name,
            Description: _description,
            CategoryId:  _categoryId,
            Price:       _price,
            Attributes:  _attributes,
            ImageUrls:   _imageUrls,
            UpdatedAt:   _updatedAt.Value));
    }

    /// <summary>
    /// Sets stock to an absolute quantity. Automatically transitions between
    /// Active ↔ OutOfStock when the quantity crosses zero.
    /// </summary>
    public void UpdateStock(int newQuantity)
    {
        if (_status == ProductStatus.Deleted)
            throw new InvalidProductOperationException(Id.Value, "Cannot update stock of a deleted product");

        if (newQuantity < 0)
            throw new DomainException("Stock quantity cannot be negative");

        var previous = _stockQuantity;
        _stockQuantity = newQuantity;
        _updatedAt     = DateTime.UtcNow;

        AddDomainEvent(new ProductStockUpdatedEvent(Id, previous, newQuantity, _updatedAt.Value));

        // auto-transition: stock depleted while active
        if (newQuantity == 0 && _status == ProductStatus.Active)
            ChangeStatus(ProductStatus.OutOfStock);

        // auto-transition: stock restored while out-of-stock
        else if (newQuantity > 0 && _status == ProductStatus.OutOfStock)
            ChangeStatus(ProductStatus.Active);
    }

    /// <summary>Publishes the product, making it visible in search results.</summary>
    public void Activate()
    {
        _status.ValidateTransitionTo(ProductStatus.Active);
        ChangeStatus(ProductStatus.Active);
    }

    /// <summary>Hides the product from search without deleting it.</summary>
    public void Deactivate()
    {
        _status.ValidateTransitionTo(ProductStatus.Inactive);
        ChangeStatus(ProductStatus.Inactive);
    }

    /// <summary>Soft-deletes the product. Terminal state — cannot be undone.</summary>
    public void Delete()
    {
        _status.ValidateTransitionTo(ProductStatus.Deleted);
        ChangeStatus(ProductStatus.Deleted);
        AddDomainEvent(new ProductDeletedEvent(Id, _updatedAt!.Value));
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void ChangeStatus(ProductStatus newStatus)
    {
        var previous = _status;
        _status    = newStatus;
        _updatedAt = DateTime.UtcNow;
        AddDomainEvent(new ProductStatusChangedEvent(Id, previous.Name, newStatus.Name, _updatedAt.Value));
    }
}
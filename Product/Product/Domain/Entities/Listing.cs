using Domain.Common;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Listing : AggregateRoot<ListingId>
{
    private CatalogItemId _catalogItemId = null!;
    private SellerId _sellerId = null!;
    private Money _price = null!;
    private int _stockQuantity;
    private ListingCondition _condition = null!;
    private ListingStatus _status = null!;
    private string? _sellerNotes;
    private DateTime _createdAt;
    private DateTime? _updatedAt;

    public CatalogItemId CatalogItemId => _catalogItemId;
    public SellerId SellerId => _sellerId;
    public Money Price => _price;
    public int StockQuantity => _stockQuantity;
    public ListingCondition Condition => _condition;
    public ListingStatus Status => _status;
    public string? SellerNotes => _sellerNotes;
    public DateTime CreatedAt => _createdAt;
    public DateTime? UpdatedAt => _updatedAt;

    private Listing() { }

    public static Listing Create(
        CatalogItemId catalogItemId,
        SellerId sellerId,
        Money price,
        int initialStock,
        ListingCondition condition,
        string? sellerNotes)
    {
        if (initialStock < 0)
            throw new DomainException("Initial stock cannot be negative");

        var createdAt = DateTime.UtcNow;
        var listing = new Listing
        {
            Id = ListingId.CreateUnique(),
            _catalogItemId = catalogItemId,
            _sellerId = sellerId,
            _price = price,
            _stockQuantity = initialStock,
            _condition = condition,
            _status = initialStock > 0 ? ListingStatus.Active : ListingStatus.OutOfStock,
            _sellerNotes = NormalizeSellerNotes(sellerNotes),
            _createdAt = createdAt
        };

        listing.AddDomainEvent(new ListingCreatedEvent(
            listing.Id,
            catalogItemId,
            sellerId,
            price,
            initialStock,
            condition,
            listing._status,
            listing._sellerNotes,
            createdAt));

        return listing;
    }

    public void ChangePrice(Money price)
    {
        EnsureNotDeleted("Cannot change price of a deleted listing");

        var previous = _price;
        _price = price;
        _updatedAt = DateTime.UtcNow;

        AddDomainEvent(new ListingPriceChangedEvent(Id, previous, price, _updatedAt.Value));
    }

    public void UpdateOfferDetails(ListingCondition condition, string? sellerNotes)
    {
        EnsureNotDeleted("Cannot update a deleted listing");

        _condition = condition;
        _sellerNotes = NormalizeSellerNotes(sellerNotes);
        _updatedAt = DateTime.UtcNow;
    }

    public void UpdateStock(int newQuantity)
    {
        EnsureNotDeleted("Cannot update stock of a deleted listing");

        if (newQuantity < 0)
            throw new DomainException("Stock quantity cannot be negative");

        SetStock(newQuantity);
    }

    public void AdjustStock(int delta)
    {
        EnsureNotDeleted("Cannot update stock of a deleted listing");

        SetStock(Math.Max(0, _stockQuantity + delta));
    }

    public void Activate()
    {
        _status.ValidateTransitionTo(ListingStatus.Active);
        ChangeStatus(ListingStatus.Active);
    }

    public void Deactivate()
    {
        _status.ValidateTransitionTo(ListingStatus.Inactive);
        ChangeStatus(ListingStatus.Inactive);
    }

    public void Delete()
    {
        _status.ValidateTransitionTo(ListingStatus.Deleted);
        ChangeStatus(ListingStatus.Deleted);
    }

    private void SetStock(int newQuantity)
    {
        var previous = _stockQuantity;
        _stockQuantity = newQuantity;
        _updatedAt = DateTime.UtcNow;

        AddDomainEvent(new ListingStockChangedEvent(Id, previous, newQuantity, _updatedAt.Value));

        if (newQuantity == 0 && Equals(_status, ListingStatus.Active))
            ChangeStatus(ListingStatus.OutOfStock);
        else if (newQuantity > 0 && Equals(_status, ListingStatus.OutOfStock))
            ChangeStatus(ListingStatus.Active);
    }

    private void ChangeStatus(ListingStatus newStatus)
    {
        var previous = _status;
        _status = newStatus;
        _updatedAt = DateTime.UtcNow;

        AddDomainEvent(new ListingStatusChangedEvent(Id, previous.Name, newStatus.Name, _updatedAt.Value));
    }

    private void EnsureNotDeleted(string message)
    {
        if (Equals(_status, ListingStatus.Deleted))
            throw new InvalidProductOperationException(Id.Value, message);
    }

    private static string? NormalizeSellerNotes(string? sellerNotes)
        => string.IsNullOrWhiteSpace(sellerNotes) ? null : sellerNotes.Trim();
}
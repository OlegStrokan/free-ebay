using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

internal sealed class ProductPersistenceService(
    IProductRepository productRepository,
    ICatalogItemRepository catalogItemRepository,
    IListingRepository listingRepository,
    IOutboxRepository outboxRepository,
    ProductDbContext dbContext,
    ILogger<ProductPersistenceService> logger) : IProductPersistenceService
{
    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
        => productRepository.GetByIdAsync(id, ct);

    public Task<CatalogItem?> GetCatalogItemByIdAsync(CatalogItemId id, CancellationToken ct = default)
        => catalogItemRepository.GetByIdAsync(id, ct);

    public Task<CatalogItem?> GetCatalogItemByGtinAsync(string gtin, CancellationToken ct = default)
        => catalogItemRepository.GetByGtinAsync(gtin, ct);

    public Task<Listing?> GetListingByIdAsync(ListingId id, CancellationToken ct = default)
        => listingRepository.GetByIdAsync(id, ct);

    public Task<bool> ActiveListingExistsAsync(
        CatalogItemId catalogItemId,
        SellerId sellerId,
        ListingId? excludedListingId = null,
        CancellationToken ct = default)
        => listingRepository.ActiveListingExistsAsync(catalogItemId, sellerId, excludedListingId, ct);

    public async Task CreateProductAsync(Product product, CancellationToken ct = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                await productRepository.AddAsync(product, ct);
                await AddOutboxMessagesAsync(product.DomainEvents, product.Id.Value.ToString(), ct);
                product.ClearDomainEvents();

                await dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                logger.LogInformation("Created product {ProductId}", product.Id.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed while creating product {ProductId}", product.Id.Value);
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task UpdateProductAsync(Product product, CancellationToken ct = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                await productRepository.UpdateAsync(product, ct);
                await AddOutboxMessagesAsync(product.DomainEvents, product.Id.Value.ToString(), ct);
                product.ClearDomainEvents();

                await dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                logger.LogDebug("Updated product {ProductId}", product.Id.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed while updating product {ProductId}", product.Id.Value);
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task CreateCatalogItemAsync(CatalogItem catalogItem, CancellationToken ct = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                await catalogItemRepository.AddAsync(catalogItem, ct);
                await AddOutboxMessagesAsync(catalogItem.DomainEvents, catalogItem.Id.Value.ToString(), ct);
                catalogItem.ClearDomainEvents();

                await dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                logger.LogInformation("Created catalog item {CatalogItemId}", catalogItem.Id.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed while creating catalog item {CatalogItemId}", catalogItem.Id.Value);
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task UpdateCatalogItemAsync(CatalogItem catalogItem, CancellationToken ct = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                await catalogItemRepository.UpdateAsync(catalogItem, ct);
                await AddOutboxMessagesAsync(catalogItem.DomainEvents, catalogItem.Id.Value.ToString(), ct);
                catalogItem.ClearDomainEvents();

                await dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                logger.LogDebug("Updated catalog item {CatalogItemId}", catalogItem.Id.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed while updating catalog item {CatalogItemId}", catalogItem.Id.Value);
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task CreateListingAsync(Listing listing, CancellationToken ct = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                await listingRepository.AddAsync(listing, ct);
                await AddOutboxMessagesAsync(listing.DomainEvents, listing.Id.Value.ToString(), ct);
                listing.ClearDomainEvents();

                await dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                logger.LogInformation("Created listing {ListingId}", listing.Id.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed while creating listing {ListingId}", listing.Id.Value);
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task UpdateListingAsync(Listing listing, CancellationToken ct = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                await listingRepository.UpdateAsync(listing, ct);
                await AddOutboxMessagesAsync(listing.DomainEvents, listing.Id.Value.ToString(), ct);
                listing.ClearDomainEvents();

                await dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                logger.LogDebug("Updated listing {ListingId}", listing.Id.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed while updating listing {ListingId}", listing.Id.Value);
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task CreateCatalogItemWithListingAsync(CatalogItem catalogItem, Listing listing, CancellationToken ct = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                await catalogItemRepository.AddAsync(catalogItem, ct);
                await listingRepository.AddAsync(listing, ct);

                await AddOutboxMessagesAsync(catalogItem.DomainEvents, catalogItem.Id.Value.ToString(), ct);
                await AddOutboxMessagesAsync(listing.DomainEvents, listing.Id.Value.ToString(), ct);
                catalogItem.ClearDomainEvents();
                listing.ClearDomainEvents();

                await dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                logger.LogInformation(
                    "Created catalog item {CatalogItemId} with listing {ListingId}",
                    catalogItem.Id.Value,
                    listing.Id.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Transaction failed while creating catalog item {CatalogItemId} with listing {ListingId}",
                    catalogItem.Id.Value,
                    listing.Id.Value);
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task UpdateCatalogItemWithListingAsync(CatalogItem catalogItem, Listing listing, CancellationToken ct = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                await catalogItemRepository.UpdateAsync(catalogItem, ct);
                await listingRepository.UpdateAsync(listing, ct);

                await AddOutboxMessagesAsync(catalogItem.DomainEvents, catalogItem.Id.Value.ToString(), ct);
                await AddOutboxMessagesAsync(listing.DomainEvents, listing.Id.Value.ToString(), ct);
                catalogItem.ClearDomainEvents();
                listing.ClearDomainEvents();

                await dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                logger.LogDebug(
                    "Updated catalog item {CatalogItemId} with listing {ListingId}",
                    catalogItem.Id.Value,
                    listing.Id.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Transaction failed while updating catalog item {CatalogItemId} with listing {ListingId}",
                    catalogItem.Id.Value,
                    listing.Id.Value);
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    private static readonly JsonSerializerOptions _outboxSerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private async Task AddOutboxMessagesAsync(
        IReadOnlyList<IDomainEvent> domainEvents,
        string aggregateId,
        CancellationToken ct)
    {
        foreach (var @event in domainEvents)
        {
            await outboxRepository.AddAsync(
                @event.EventId,
                @event.GetType().Name,
                JsonSerializer.Serialize(@event, @event.GetType(), _outboxSerializerOptions),
                @event.OccurredOn,
                aggregateId,
                ct);
        }
    }
}

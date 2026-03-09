using System.Data;
using System.Text.Json;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

internal sealed class ProductPersistenceService(
    IProductRepository productRepository,
    IOutboxRepository outboxRepository,
    ProductDbContext dbContext,
    ILogger<ProductPersistenceService> logger) : IProductPersistenceService
{
    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
        => productRepository.GetByIdAsync(id, ct);

    public async Task CreateProductAsync(Product product, CancellationToken ct = default)
    {
        await using var tx = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            await productRepository.AddAsync(product, ct);

            foreach (var @event in product.DomainEvents)
            {
                await outboxRepository.AddAsync(
                    @event.EventId,
                    @event.GetType().Name,
                    JsonSerializer.Serialize(@event, @event.GetType()),
                    @event.OccurredOn,
                    product.Id.Value.ToString(),
                    ct);
            }

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
    }

    public async Task UpdateProductAsync(Product product, CancellationToken ct = default)
    {
        await using var tx = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            await productRepository.UpdateAsync(product, ct);

            foreach (var @event in product.DomainEvents)
            {
                await outboxRepository.AddAsync(
                    @event.EventId,
                    @event.GetType().Name,
                    JsonSerializer.Serialize(@event, @event.GetType()),
                    @event.OccurredOn,
                    product.Id.Value.ToString(),
                    ct);
            }

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
    }
}

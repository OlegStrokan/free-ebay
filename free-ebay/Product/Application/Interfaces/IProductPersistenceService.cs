using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Interfaces;

public interface IProductPersistenceService
{
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default);
    Task CreateProductAsync(Product product, CancellationToken ct = default);
    Task UpdateProductAsync(Product product, CancellationToken ct = default);
}

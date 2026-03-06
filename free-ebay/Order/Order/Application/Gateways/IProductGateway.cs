using Application.DTOs;

namespace Application.Gateways;

public interface IProductGateway
{
        Task<IReadOnlyList<ProductPriceDto>> GetCurrentPricesAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken);
}

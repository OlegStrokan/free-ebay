using Application.DTOs;
using Application.Gateways;
using Application.Gateways.Exceptions;
using Grpc.Core;
using Protos.Product;
using StatusCode = Grpc.Core.StatusCode;

namespace Infrastructure.Gateways;

public sealed class ProductGateway(
    ProductService.ProductServiceClient client,
    ILogger<ProductGateway> logger) : IProductGateway
{
    public async Task<IReadOnlyList<ProductPriceDto>> GetCurrentPricesAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var ids = productIds.Select(id => id.ToString()).ToList();

        var request = new GetProductPricesRequest();
        request.ProductIds.AddRange(ids);

        try
        {
            var response = await client.GetProductPricesAsync(request, cancellationToken: cancellationToken);

            if (response.NotFoundIds.Count > 0)
                throw new ProductNotFoundException(response.NotFoundIds);

            var prices = response.Prices.Select(p => new ProductPriceDto(
                Guid.Parse(p.ProductId),
                p.Price.Units + p.Price.Nanos / 1_000_000_000m,
                p.Currency)).ToList();

            logger.LogInformation(
                "Fetched authoritative prices for {Count} product(s) from Product Service",
                prices.Count);

            return prices;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new ProductNotFoundException([ex.Status.Detail]);
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            throw new GatewayUnavailableException(
                $"Product Service unavailable. gRPC={ex.StatusCode}: {ex.Status.Detail}", ex);
        }
    }
}

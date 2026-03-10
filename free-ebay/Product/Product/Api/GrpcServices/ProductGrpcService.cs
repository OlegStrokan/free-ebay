using Api.Mappers;
using Application.DTOs;
using Application.Queries.GetProduct;
using Application.Queries.GetProductPrices;
using Application.Queries.GetProducts;
using Application.Queries.GetSellerProducts;
using FluentValidation;
using Grpc.Core;
using MediatR;
using Protos.Product;

namespace Api.GrpcServices;

public class ProductGrpcService(
    IMediator mediator,
    ILogger<ProductGrpcService> logger,
    IValidator<GetProductPricesRequest> getPricesValidator,
    IValidator<GetProductsRequest> getProductsValidator,
    IValidator<GetProductRequest> getProductValidator)
    : ProductService.ProductServiceBase
{
    public override async Task<GetProductPricesResponse> GetProductPrices(
        GetProductPricesRequest request,
        ServerCallContext context)
    {
        try
        {
            var validation = await getPricesValidator.ValidateAsync(request, context.CancellationToken);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var ids = request.ProductIds
                .Select(id => Guid.Parse(id))
                .ToList();

            var result = await mediator.Send(new GetProductPricesQuery(ids), context.CancellationToken);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.Internal, result.Errors[0]));

            var response = new GetProductPricesResponse();
            response.Prices.AddRange(result.Value!.Select(MapToProductPrice));
            return response;
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(GetProductPrices));
            throw;
        }
    }

    public override async Task<GetProductsResponse> GetProducts(
        GetProductsRequest request,
        ServerCallContext context)
    {
        try
        {
            var validation = await getProductsValidator.ValidateAsync(request, context.CancellationToken);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var ids = request.ProductIds
                .Select(id => Guid.Parse(id))
                .ToList();

            var result = await mediator.Send(new GetProductsQuery(ids), context.CancellationToken);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.Internal, result.Errors[0]));

            var foundIds = result.Value!.Select(p => p.ProductId.ToString()).ToHashSet();
            var notFoundIds = ids
                .Where(id => !foundIds.Contains(id.ToString()))
                .Select(id => id.ToString())
                .ToList();

            var response = new GetProductsResponse();
            response.Products.AddRange(result.Value!.Select(MapToProductDetail));
            response.NotFoundIds.AddRange(notFoundIds);
            return response;
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(GetProducts));
            throw;
        }
    }

    public override async Task<GetProductResponse> GetProduct(
        GetProductRequest request,
        ServerCallContext context)
    {
        try
        {
            var validation = await getProductValidator.ValidateAsync(request, context.CancellationToken);
            if (!validation.IsValid)
                return new GetProductResponse
                {
                    Success = false,
                    ErrorMessage = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))
                };

            if (!Guid.TryParse(request.ProductId, out var productId))
                return new GetProductResponse { Success = false, ErrorMessage = "Invalid product ID format." };

            var result = await mediator.Send(new GetProductQuery(productId), context.CancellationToken);

            if (!result.IsSuccess)
                return new GetProductResponse { Success = false, ErrorMessage = result.Errors[0] };

            return new GetProductResponse
            {
                Success = true,
                Product = MapToProductDetail(result.Value!)
            };
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(GetProduct));
            throw;
        }
    }

    private void HandleException(Exception ex, string methodName)
    {
        if (ex is FormatException)
        {
            logger.LogWarning(ex, "Invalid GUID format in {Method}", methodName);
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ID format."));
        }

        logger.LogError(ex, "Error during {Method}", methodName);
        throw new RpcException(new Status(StatusCode.Internal, $"Internal error in {methodName}"));
    }

    private static ProductPrice MapToProductPrice(ProductPriceDto dto) => new()
    {
        ProductId = dto.ProductId.ToString(),
        Price = dto.Price.ToDecimalValue(),
        Currency = dto.Currency
    };

    private static ProductDetail MapToProductDetail(ProductDetailDto dto)
    {
        var detail = new ProductDetail
        {
            ProductId = dto.ProductId.ToString(),
            Name = dto.Name,
            Description = dto.Description,
            CategoryId = dto.CategoryId.ToString(),
            CategoryName = dto.CategoryName,
            Price = dto.Price.ToDecimalValue(),
            Currency = dto.Currency,
            Stock = dto.StockQuantity
        };

        detail.Attributes.AddRange(dto.Attributes.Select(a => new ProductAttributeProto
        {
            Key = a.Key,
            Value = a.Value
        }));
        detail.ImageUrls.AddRange(dto.ImageUrls);

        return detail;
    }
}

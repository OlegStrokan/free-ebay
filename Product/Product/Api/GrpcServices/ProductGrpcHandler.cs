using Api.Mappers;
using Application.Commands.ActivateProduct;
using Application.Commands.AdjustProductStock;
using Application.Commands.CreateProduct;
using Application.Commands.DeactivateProduct;
using Application.Commands.DeleteProduct;
using Application.Commands.UpdateProduct;
using Application.Commands.UpdateProductStock;
using Application.DTOs;
using Application.Queries.GetProduct;
using Application.Queries.GetProductPrices;
using Application.Queries.GetProducts;
using Grpc.Core;
using MediatR;
using Protos.Product;

namespace Api.GrpcServices;

public sealed class ProductGrpcHandler(IMediator mediator, ILogger<ProductGrpcHandler> logger)
    : GrpcHandlerBase(logger)
{
    public async Task<GetProductPricesResponse> GetProductPrices(
        GetProductPricesRequest request, CancellationToken ct)
    {
        try
        {
            var ids = request.ProductIds.Select(Guid.Parse).ToList();
            var result = await mediator.Send(new GetProductPricesQuery(ids), ct);
            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.Internal, result.Errors[0]));

            var response = new GetProductPricesResponse();
            response.Prices.AddRange(result.Value!.Select(MapToProductPrice));
            return response;
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(GetProductPrices)); throw; }
    }

    public async Task<GetProductsResponse> GetProducts(
        GetProductsRequest request, CancellationToken ct)
    {
        try
        {
            var ids = request.ProductIds.Select(Guid.Parse).ToList();
            var result = await mediator.Send(new GetProductsQuery(ids), ct);
            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.Internal, result.Errors[0]));

            var foundIds = result.Value!.Select(p => p.ProductId.ToString()).ToHashSet();
            var notFoundIds = ids.Where(id => !foundIds.Contains(id.ToString()))
                                 .Select(id => id.ToString()).ToList();

            var response = new GetProductsResponse();
            response.Products.AddRange(result.Value!.Select(MapToProductDetail));
            response.NotFoundIds.AddRange(notFoundIds);
            return response;
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(GetProducts)); throw; }
    }

    public async Task<GetProductResponse> GetProduct(
        GetProductRequest request, CancellationToken ct)
    {
        try
        {
            var product = await mediator.Send(new GetProductQuery(Guid.Parse(request.ProductId)), ct);
            return new GetProductResponse { Product = MapToProductDetail(product) };
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(GetProduct)); throw; }
    }

    public async Task<CreateProductResponse> CreateProduct(
        CreateProductRequest request, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new CreateProductCommand(
                SellerId: Guid.Parse(request.SellerId),
                Name: request.Name,
                Description: request.Description,
                CategoryId: Guid.Parse(request.CategoryId),
                Price: request.Price.ToDecimal(),
                Currency: request.Currency,
                InitialStock: request.InitialStock,
                Attributes: request.Attributes.Select(a => new ProductAttributeDto(a.Key, a.Value)).ToList(),
                ImageUrls: request.ImageUrls.ToList()), ct);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new CreateProductResponse { ProductId = result.Value!.ToString() };
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(CreateProduct)); throw; }
    }

    public async Task<UpdateProductResponse> UpdateProduct(
        UpdateProductRequest request, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new UpdateProductCommand(
                ProductId: Guid.Parse(request.ProductId),
                Name: request.Name,
                Description: request.Description,
                CategoryId: Guid.Parse(request.CategoryId),
                Price: request.Price.ToDecimal(),
                Currency: request.Currency,
                Attributes: request.Attributes.Select(a => new ProductAttributeDto(a.Key, a.Value)).ToList(),
                ImageUrls: request.ImageUrls.ToList()), ct);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new UpdateProductResponse();
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(UpdateProduct)); throw; }
    }

    public async Task<DeleteProductResponse> DeleteProduct(
        DeleteProductRequest request, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(
                new DeleteProductCommand(Guid.Parse(request.ProductId)), ct);
            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new DeleteProductResponse();
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(DeleteProduct)); throw; }
    }

    public async Task<ActivateProductResponse> ActivateProduct(
        ActivateProductRequest request, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(
                new ActivateProductCommand(Guid.Parse(request.ProductId)), ct);
            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new ActivateProductResponse();
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(ActivateProduct)); throw; }
    }

    public async Task<DeactivateProductResponse> DeactivateProduct(
        DeactivateProductRequest request, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(
                new DeactivateProductCommand(Guid.Parse(request.ProductId)), ct);
            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new DeactivateProductResponse();
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(DeactivateProduct)); throw; }
    }

    public async Task<UpdateProductStockResponse> UpdateProductStock(
        UpdateProductStockRequest request, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(
                new UpdateProductStockCommand(Guid.Parse(request.ProductId), request.NewQuantity), ct);
            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new UpdateProductStockResponse();
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(UpdateProductStock)); throw; }
    }

    internal static ProductPrice MapToProductPrice(ProductPriceDto dto) => new()
    {
        ProductId = dto.ProductId.ToString(),
        Price = dto.Price.ToDecimalValue(),
        Currency = dto.Currency
    };

    internal static ProductDetail MapToProductDetail(ProductDetailDto dto)
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
            Stock = dto.StockQuantity,
            SellerId = dto.SellerId.ToString(),
            Status = dto.Status
        };
        detail.Attributes.AddRange(
            dto.Attributes.Select(a => new ProductAttributeProto { Key = a.Key, Value = a.Value }));
        detail.ImageUrls.AddRange(dto.ImageUrls);
        return detail;
    }
}

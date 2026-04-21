using Api.Mappers;
using Application.Commands.ActivateProduct;
using Application.Commands.CreateProduct;
using Application.Commands.DeactivateProduct;
using Application.Commands.DeleteProduct;
using Application.Commands.UpdateProduct;
using Application.Commands.UpdateProductStock;
using Application.DTOs;
using Application.Queries.GetProduct;
using Application.Queries.GetProductPrices;
using Application.Queries.GetProducts;
using Application.Queries.GetSellerProducts;
using Domain.Exceptions;
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
    IValidator<GetProductRequest> getProductValidator,
    IValidator<CreateProductRequest> createValidator,
    IValidator<UpdateProductRequest> updateValidator,
    IValidator<DeleteProductRequest> deleteValidator,
    IValidator<ActivateProductRequest> activateValidator,
    IValidator<DeactivateProductRequest> deactivateValidator,
    IValidator<UpdateProductStockRequest> updateStockValidator)
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
                .Select(Guid.Parse)
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
                .Select(Guid.Parse)
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
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var productId = Guid.Parse(request.ProductId);

            var product = await mediator.Send(new GetProductQuery(productId), context.CancellationToken);

            return new GetProductResponse { Product = MapToProductDetail(product) };
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(GetProduct));
            throw;
        }
    }

    public override async Task<CreateProductResponse> CreateProduct(
        CreateProductRequest request,
        ServerCallContext context)
    {
        try
        {
            var validation = await createValidator.ValidateAsync(request, context.CancellationToken);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var command = new CreateProductCommand(
                SellerId: Guid.Parse(request.SellerId),
                Name: request.Name,
                Description: request.Description,
                CategoryId: Guid.Parse(request.CategoryId),
                Price: request.Price.ToDecimal(),
                Currency: request.Currency,
                InitialStock: request.InitialStock,
                Attributes: request.Attributes.Select(a => new ProductAttributeDto(a.Key, a.Value)).ToList(),
                ImageUrls: request.ImageUrls.ToList());

            var result = await mediator.Send(command, context.CancellationToken);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));

            return new CreateProductResponse { ProductId = result.Value!.ToString() };
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(CreateProduct));
            throw;
        }
    }

    public override async Task<UpdateProductResponse> UpdateProduct(
        UpdateProductRequest request,
        ServerCallContext context)
    {
        try
        {
            var validation = await updateValidator.ValidateAsync(request, context.CancellationToken);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var command = new UpdateProductCommand(
                ProductId: Guid.Parse(request.ProductId),
                Name: request.Name,
                Description: request.Description,
                CategoryId: Guid.Parse(request.CategoryId),
                Price: request.Price.ToDecimal(),
                Currency: request.Currency,
                Attributes: request.Attributes.Select(a => new ProductAttributeDto(a.Key, a.Value)).ToList(),
                ImageUrls: request.ImageUrls.ToList());

            var result = await mediator.Send(command, context.CancellationToken);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));

            return new UpdateProductResponse();
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(UpdateProduct));
            throw;
        }
    }

    public override async Task<DeleteProductResponse> DeleteProduct(
        DeleteProductRequest request,
        ServerCallContext context)
    {
        try
        {
            var validation = await deleteValidator.ValidateAsync(request, context.CancellationToken);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var result = await mediator.Send(
                new DeleteProductCommand(Guid.Parse(request.ProductId)),
                context.CancellationToken);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));

            return new DeleteProductResponse();
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(DeleteProduct));
            throw;
        }
    }

    public override async Task<ActivateProductResponse> ActivateProduct(
        ActivateProductRequest request,
        ServerCallContext context)
    {
        try
        {
            var validation = await activateValidator.ValidateAsync(request, context.CancellationToken);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var result = await mediator.Send(
                new ActivateProductCommand(Guid.Parse(request.ProductId)),
                context.CancellationToken);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));

            return new ActivateProductResponse();
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(ActivateProduct));
            throw;
        }
    }

    public override async Task<DeactivateProductResponse> DeactivateProduct(
        DeactivateProductRequest request,
        ServerCallContext context)
    {
        try
        {
            var validation = await deactivateValidator.ValidateAsync(request, context.CancellationToken);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var result = await mediator.Send(
                new DeactivateProductCommand(Guid.Parse(request.ProductId)),
                context.CancellationToken);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));

            return new DeactivateProductResponse();
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(DeactivateProduct));
            throw;
        }
    }

    public override async Task<UpdateProductStockResponse> UpdateProductStock(
        UpdateProductStockRequest request,
        ServerCallContext context)
    {
        try
        {
            var validation = await updateStockValidator.ValidateAsync(request, context.CancellationToken);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var result = await mediator.Send(
                new UpdateProductStockCommand(Guid.Parse(request.ProductId), request.NewQuantity),
                context.CancellationToken);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));

            return new UpdateProductStockResponse();
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(UpdateProductStock));
            throw;
        }
    }

    private void HandleException(Exception ex, string methodName)
    {
        if (ex is ProductNotFoundException notFound)
        {
            logger.LogWarning(ex, "Product {ProductId} not found in {Method}", notFound.ProductId, methodName);
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }

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

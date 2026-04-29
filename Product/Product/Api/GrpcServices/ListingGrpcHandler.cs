using Api.Mappers;
using Application.Commands.ActivateListing;
using Application.Commands.ChangeListingPrice;
using Application.Commands.CreateCatalogItem;
using Application.Commands.CreateCatalogItemWithListing;
using Application.Commands.CreateListing;
using Application.Commands.DeactivateListing;
using Application.Commands.DeleteListing;
using Application.Commands.UpdateCatalogItem;
using Application.Commands.UpdateCatalogItemAndListing;
using Application.Commands.UpdateListingStock;
using Application.DTOs;
using Application.Queries.GetListing;
using Application.Queries.GetListingPrices;
using Application.Queries.GetListings;
using Application.Queries.GetSellerListings;
using FluentValidation;
using Grpc.Core;
using MediatR;
using Protos.Product;

namespace Api.GrpcServices;

public sealed class ListingGrpcHandler(
    IMediator mediator,
    ILogger<ListingGrpcHandler> logger,
    IValidator<GetListingPricesRequest> getListingPricesValidator,
    IValidator<GetListingsRequest> getListingsValidator,
    IValidator<GetListingRequest> getListingValidator,
    IValidator<CreateCatalogItemWithListingRequest> createCatalogItemWithListingValidator,
    IValidator<UpdateCatalogItemAndListingRequest> updateCatalogItemAndListingValidator,
    IValidator<DeleteListingRequest> deleteListingValidator,
    IValidator<ActivateListingRequest> activateListingValidator,
    IValidator<DeactivateListingRequest> deactivateListingValidator,
    IValidator<UpdateListingStockRequest> updateListingStockValidator)
    : GrpcHandlerBase(logger)
{
    public async Task<GetListingPricesResponse> GetListingPrices(
        GetListingPricesRequest request, CancellationToken ct)
    {
        try
        {
            var validation = await getListingPricesValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var ids = request.ListingIds.Select(Guid.Parse).ToList();
            var result = await mediator.Send(new GetListingPricesQuery(ids), ct);
            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.Internal, result.Errors[0]));

            var response = new GetListingPricesResponse();
            response.Prices.AddRange(result.Value!.Select(MapToListingPrice));
            return response;
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(GetListingPrices)); throw; }
    }

    public async Task<GetListingsResponse> GetListings(
        GetListingsRequest request, CancellationToken ct)
    {
        try
        {
            var validation = await getListingsValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var ids = request.ListingIds.Select(Guid.Parse).ToList();
            var result = await mediator.Send(new GetListingsQuery(ids), ct);
            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.Internal, result.Errors[0]));

            var foundIds = result.Value!.Select(p => p.ProductId.ToString()).ToHashSet();
            var notFoundIds = ids.Where(id => !foundIds.Contains(id.ToString()))
                                 .Select(id => id.ToString()).ToList();

            var response = new GetListingsResponse();
            response.Listings.AddRange(result.Value!.Select(MapToListingDetail));
            response.NotFoundIds.AddRange(notFoundIds);
            return response;
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(GetListings)); throw; }
    }

    public async Task<GetListingResponse> GetListing(
        GetListingRequest request, CancellationToken ct)
    {
        try
        {
            var validation = await getListingValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var listing = await mediator.Send(new GetListingQuery(Guid.Parse(request.ListingId)), ct);
            return new GetListingResponse { Listing = MapToListingDetail(listing) };
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(GetListing)); throw; }
    }

    public async Task<GetSellerListingsResponse> GetSellerListings(
        GetSellerListingsRequest request, CancellationToken ct)
    {
        try
        {
            var page = request.Page > 0 ? request.Page : 1;
            var size = request.Size > 0 ? request.Size : 20;
            var result = await mediator.Send(new GetSellerListingsQuery(Guid.Parse(request.SellerId), page, size), ct);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.Internal, result.Errors[0]));

            var paged = result.Value!;
            var response = new GetSellerListingsResponse { TotalCount = paged.TotalCount };
            response.Listings.AddRange(paged.Items.Select(s => new ListingDetail
            {
                ListingId = s.ProductId.ToString(),
                Name = s.Name,
                CategoryId = string.Empty,
                CategoryName = s.CategoryName,
                Price = s.Price.ToDecimalValue(),
                Currency = s.Currency,
                Stock = s.StockQuantity,
                SellerId = s.SellerId.ToString(),
                Status = s.Status,
                CatalogItemId = s.CatalogItemId.ToString(),
                Condition = s.Condition
            }));
            return response;
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(GetSellerListings)); throw; }
    }

    public async Task<CreateCatalogItemResponse> CreateCatalogItem(
        CreateCatalogItemRequest request, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new CreateCatalogItemCommand(
                Name: request.Name,
                Description: request.Description,
                CategoryId: Guid.Parse(request.CategoryId),
                Gtin: string.IsNullOrWhiteSpace(request.Gtin) ? null : request.Gtin,
                Attributes: request.Attributes.Select(a => new ProductAttributeDto(a.Key, a.Value)).ToList(),
                ImageUrls: request.ImageUrls.ToList()), ct);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new CreateCatalogItemResponse { CatalogItemId = result.Value!.ToString() };
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(CreateCatalogItem)); throw; }
    }

    public async Task<UpdateCatalogItemResponse> UpdateCatalogItem(
        UpdateCatalogItemRequest request, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new UpdateCatalogItemCommand(
                CatalogItemId: Guid.Parse(request.CatalogItemId),
                Name: request.Name,
                Description: request.Description,
                CategoryId: Guid.Parse(request.CategoryId),
                Gtin: string.IsNullOrWhiteSpace(request.Gtin) ? null : request.Gtin,
                Attributes: request.Attributes.Select(a => new ProductAttributeDto(a.Key, a.Value)).ToList(),
                ImageUrls: request.ImageUrls.ToList()), ct);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new UpdateCatalogItemResponse();
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(UpdateCatalogItem)); throw; }
    }

    public async Task<CreateCatalogItemWithListingResponse> CreateCatalogItemWithListing(
        CreateCatalogItemWithListingRequest request, CancellationToken ct)
    {
        try
        {
            var validation = await createCatalogItemWithListingValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var result = await mediator.Send(new CreateCatalogItemWithListingCommand(
                SellerId: Guid.Parse(request.SellerId),
                Name: request.Name,
                Description: request.Description,
                CategoryId: Guid.Parse(request.CategoryId),
                Price: request.Price.ToDecimal(),
                Currency: request.Currency,
                InitialStock: request.InitialStock,
                Attributes: request.Attributes.Select(a => new ProductAttributeDto(a.Key, a.Value)).ToList(),
                ImageUrls: request.ImageUrls.ToList(),
                Gtin: string.IsNullOrWhiteSpace(request.Gtin) ? null : request.Gtin,
                Condition: string.IsNullOrWhiteSpace(request.Condition) ? "New" : request.Condition,
                SellerNotes: string.IsNullOrWhiteSpace(request.SellerNotes) ? null : request.SellerNotes), ct);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new CreateCatalogItemWithListingResponse { ListingId = result.Value!.ToString() };
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(CreateCatalogItemWithListing)); throw; }
    }

    public async Task<UpdateCatalogItemAndListingResponse> UpdateCatalogItemAndListing(
        UpdateCatalogItemAndListingRequest request, CancellationToken ct)
    {
        try
        {
            var validation = await updateCatalogItemAndListingValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var result = await mediator.Send(new UpdateCatalogItemAndListingCommand(
                ListingId: Guid.Parse(request.ListingId),
                Name: request.Name,
                Description: request.Description,
                CategoryId: Guid.Parse(request.CategoryId),
                Price: request.Price.ToDecimal(),
                Currency: request.Currency,
                Attributes: request.Attributes.Select(a => new ProductAttributeDto(a.Key, a.Value)).ToList(),
                ImageUrls: request.ImageUrls.ToList(),
                Gtin: string.IsNullOrWhiteSpace(request.Gtin) ? null : request.Gtin,
                Condition: string.IsNullOrWhiteSpace(request.Condition) ? null : request.Condition,
                SellerNotes: string.IsNullOrWhiteSpace(request.SellerNotes) ? null : request.SellerNotes), ct);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new UpdateCatalogItemAndListingResponse();
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(UpdateCatalogItemAndListing)); throw; }
    }

    public async Task<CreateListingResponse> CreateListing(
        CreateListingRequest request, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new CreateListingCommand(
                CatalogItemId: Guid.Parse(request.CatalogItemId),
                SellerId: Guid.Parse(request.SellerId),
                Price: request.Price.ToDecimal(),
                Currency: request.Currency,
                InitialStock: request.InitialStock,
                Condition: string.IsNullOrWhiteSpace(request.Condition) ? "New" : request.Condition,
                SellerNotes: string.IsNullOrWhiteSpace(request.SellerNotes) ? null : request.SellerNotes), ct);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new CreateListingResponse { ListingId = result.Value!.ToString() };
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(CreateListing)); throw; }
    }

    public async Task<ActivateListingResponse> ActivateListing(
        ActivateListingRequest request, CancellationToken ct)
    {
        try
        {
            var validation = await activateListingValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var result = await mediator.Send(new ActivateListingCommand(Guid.Parse(request.ListingId)), ct);
            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new ActivateListingResponse();
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(ActivateListing)); throw; }
    }

    public async Task<DeactivateListingResponse> DeactivateListing(
        DeactivateListingRequest request, CancellationToken ct)
    {
        try
        {
            var validation = await deactivateListingValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var result = await mediator.Send(new DeactivateListingCommand(Guid.Parse(request.ListingId)), ct);
            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new DeactivateListingResponse();
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(DeactivateListing)); throw; }
    }

    public async Task<DeleteListingResponse> DeleteListing(
        DeleteListingRequest request, CancellationToken ct)
    {
        try
        {
            var validation = await deleteListingValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var result = await mediator.Send(new DeleteListingCommand(Guid.Parse(request.ListingId)), ct);
            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new DeleteListingResponse();
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(DeleteListing)); throw; }
    }

    public async Task<UpdateListingStockResponse> UpdateListingStock(
        UpdateListingStockRequest request, CancellationToken ct)
    {
        try
        {
            var validation = await updateListingStockValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var result = await mediator.Send(
                new UpdateListingStockCommand(Guid.Parse(request.ListingId), request.NewQuantity), ct);
            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new UpdateListingStockResponse();
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(UpdateListingStock)); throw; }
    }

    public async Task<ChangeListingPriceResponse> ChangeListingPrice(
        ChangeListingPriceRequest request, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(
                new ChangeListingPriceCommand(Guid.Parse(request.ListingId), request.Price.ToDecimal(), request.Currency), ct);
            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, result.Errors[0]));
            return new ChangeListingPriceResponse();
        }
        catch (Exception ex) when (ex is not RpcException) { HandleException(ex, nameof(ChangeListingPrice)); throw; }
    }

    internal static ListingPrice MapToListingPrice(ProductPriceDto dto) => new()
    {
        ListingId = dto.ProductId.ToString(),
        Price = dto.Price.ToDecimalValue(),
        Currency = dto.Currency
    };

    internal static ListingDetail MapToListingDetail(ProductDetailDto dto)
    {
        var detail = new ListingDetail
        {
            ListingId = dto.ProductId.ToString(),
            Name = dto.Name,
            Description = dto.Description,
            CategoryId = dto.CategoryId.ToString(),
            CategoryName = dto.CategoryName,
            Price = dto.Price.ToDecimalValue(),
            Currency = dto.Currency,
            Stock = dto.StockQuantity,
            CatalogItemId = dto.CatalogItemId == default ? string.Empty : dto.CatalogItemId.ToString(),
            SellerId = dto.SellerId.ToString(),
            Status = dto.Status,
            Condition = dto.Condition,
            Gtin = dto.Gtin ?? string.Empty,
            SellerNotes = dto.SellerNotes ?? string.Empty
        };
        detail.Attributes.AddRange(
            dto.Attributes.Select(a => new ListingAttributeProto { Key = a.Key, Value = a.Value }));
        detail.ImageUrls.AddRange(dto.ImageUrls);
        return detail;
    }
}

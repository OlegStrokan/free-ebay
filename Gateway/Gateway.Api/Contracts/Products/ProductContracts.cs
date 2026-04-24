namespace Gateway.Api.Contracts.Products;

public sealed record GetProductsRequest(IReadOnlyList<string> ProductIds);
public sealed record GetProductPricesRequest(IReadOnlyList<string> ProductIds);

public sealed record ProductDetailResponse(
    string ProductId,
    string Name,
    string Description,
    string CategoryId,
    string CategoryName,
    decimal Price,
    string Currency,
    int Stock,
    IReadOnlyList<ProductAttributeResponse> Attributes,
    IReadOnlyList<string> ImageUrls);

public sealed record ProductAttributeResponse(string Key, string Value);

public sealed record ProductPriceResponse(string ProductId, decimal Price, string Currency);

public sealed record GetProductsResponse(
    IReadOnlyList<ProductDetailResponse> Products,
    IReadOnlyList<string> NotFoundIds);

public sealed record GetProductPricesResponse(
    IReadOnlyList<ProductPriceResponse> Prices,
    IReadOnlyList<string> NotFoundIds);

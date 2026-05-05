using Application.Common;
using Application.DTOs;
using MediatR;

namespace Application.Queries.GetListingsForCatalogItem;

public sealed record GetListingsForCatalogItemQuery(
    Guid CatalogItemId,
    int Page,
    int Size,
    string SortBy,
    string? ConditionFilter) : IRequest<Result<PagedResult<ProductDetailDto>>>;

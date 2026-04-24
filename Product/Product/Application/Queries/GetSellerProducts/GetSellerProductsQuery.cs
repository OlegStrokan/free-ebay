using Application.Common;
using Application.DTOs;
using MediatR;

namespace Application.Queries.GetSellerProducts;

public sealed record GetSellerProductsQuery(
    Guid SellerId,
    int Page,
    int Size) : IRequest<Result<PagedResult<ProductSummaryDto>>>;

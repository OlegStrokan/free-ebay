using Application.Common;
using Application.DTOs;
using MediatR;

namespace Application.Queries.GetSellerListings;

public sealed record GetSellerListingsQuery(
    Guid SellerId,
    int Page,
    int Size) : IRequest<Result<PagedResult<ProductSummaryDto>>>;

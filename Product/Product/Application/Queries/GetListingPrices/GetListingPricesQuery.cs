using Application.Common;
using Application.DTOs;
using MediatR;

namespace Application.Queries.GetListingPrices;

public sealed record GetListingPricesQuery(List<Guid> ListingIds) : IRequest<Result<List<ProductPriceDto>>>;

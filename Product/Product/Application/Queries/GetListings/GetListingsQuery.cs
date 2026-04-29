using Application.Common;
using Application.DTOs;
using MediatR;

namespace Application.Queries.GetListings;

public sealed record GetListingsQuery(List<Guid> ListingIds) : IRequest<Result<List<ProductDetailDto>>>;

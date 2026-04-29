using Application.DTOs;
using MediatR;

namespace Application.Queries.GetListing;

public sealed record GetListingQuery(Guid ListingId) : IRequest<ProductDetailDto>;

using Application.Common;
using Application.DTOs;
using MediatR;

namespace Application.Queries.GetProductPrices;

public sealed record GetProductPricesQuery(List<Guid> ProductIds) : IRequest<Result<List<ProductPriceDto>>>;

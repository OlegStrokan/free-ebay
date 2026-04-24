using Application.Common;
using Application.DTOs;
using MediatR;

namespace Application.Queries.GetProducts;

public sealed record GetProductsQuery(List<Guid> ProductIds) : IRequest<Result<List<ProductDetailDto>>>;

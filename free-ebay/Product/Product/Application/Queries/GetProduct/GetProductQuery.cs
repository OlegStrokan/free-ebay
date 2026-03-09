using Application.Common;
using Application.DTOs;
using MediatR;

namespace Application.Queries.GetProduct;

public sealed record GetProductQuery(Guid ProductId) : IRequest<Result<ProductDetailDto>>;

using Application.DTOs;
using MediatR;

namespace Application.Queries.GetProduct;

public sealed record GetProductQuery(Guid ProductId) : IRequest<ProductDetailDto>;

using Application.Common;

namespace Application.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid ProductId) : ICommand<Result>;

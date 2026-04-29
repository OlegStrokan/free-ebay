using Application.Common;

namespace Application.Commands.DeactivateProduct;

public sealed record DeactivateProductCommand(Guid ProductId) : ICommand<Result>;

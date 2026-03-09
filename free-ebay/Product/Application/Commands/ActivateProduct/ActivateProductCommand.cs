using Application.Common;

namespace Application.Commands.ActivateProduct;

public sealed record ActivateProductCommand(Guid ProductId) : ICommand<Result>;

using Application.Common;

namespace Application.Commands.AdjustProductStock;

public sealed record AdjustProductStockCommand(Guid ProductId, int Delta) : ICommand<Result>;

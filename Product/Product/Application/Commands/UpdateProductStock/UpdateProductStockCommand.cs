using Application.Common;

namespace Application.Commands.UpdateProductStock;

public sealed record UpdateProductStockCommand(Guid ProductId, int NewQuantity) : ICommand<Result>;

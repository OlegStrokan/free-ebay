using Application.Common;

namespace Application.Commands.CancelB2BOrder;

public record CancelB2BOrderCommand(
    Guid B2BOrderId,
    List<string> Reasons) : ICommand<Result>;

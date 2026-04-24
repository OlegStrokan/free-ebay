using Application.Common;
using Application.DTOs;

namespace Application.Commands.ReconcilePendingPayments;

public sealed record ReconcilePendingPaymentsCommand(
    int OlderThanMinutes = 15,
    int BatchSize = 100) : ICommand<Result<ReconciliationResultDto>>;
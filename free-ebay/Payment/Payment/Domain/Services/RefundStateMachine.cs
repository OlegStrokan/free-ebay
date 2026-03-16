using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Services;

public static class RefundStateMachine
{
    private static readonly HashSet<(RefundStatus From, RefundStatus To)> AllowedTransitions =
    [
        (RefundStatus.Requested, RefundStatus.PendingProviderConfirmation),
        (RefundStatus.Requested, RefundStatus.Succeeded),
        (RefundStatus.Requested, RefundStatus.Failed),
        (RefundStatus.PendingProviderConfirmation, RefundStatus.Succeeded),
        (RefundStatus.PendingProviderConfirmation, RefundStatus.Failed),
        (RefundStatus.Failed, RefundStatus.PendingProviderConfirmation),
    ];

    public static bool CanTransition(RefundStatus from, RefundStatus to)
    {
        if (from == to)
        {
            return true;
        }

        return AllowedTransitions.Contains((from, to));
    }

    public static void EnsureCanTransition(RefundStatus from, RefundStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidRefundStateTransitionException(from, to);
        }
    }
}
using Domain.Enums;

namespace Domain.Exceptions;

public sealed class InvalidRefundStateTransitionException(RefundStatus from, RefundStatus to)
    : DomainException($"Invalid refund state transition from '{from}' to '{to}'");
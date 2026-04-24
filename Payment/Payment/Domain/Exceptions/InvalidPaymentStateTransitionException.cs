using Domain.Enums;

namespace Domain.Exceptions;

public sealed class InvalidPaymentStateTransitionException(PaymentStatus from, PaymentStatus to)
    : DomainException($"Invalid payment state transition from '{from}' to '{to}'");
using Domain.Enums;
using Domain.Exceptions;
using Domain.Services;

namespace Domain.Tests.Services;

public class PaymentStateMachineTests
{
    #region CanTransition – same status (self-loop)

    [Theory]
    [InlineData(PaymentStatus.Created)]
    [InlineData(PaymentStatus.PendingProviderConfirmation)]
    [InlineData(PaymentStatus.Succeeded)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.RefundPending)]
    [InlineData(PaymentStatus.Refunded)]
    [InlineData(PaymentStatus.RefundFailed)]
    public void CanTransition_SameStatus_ShouldReturnTrue(PaymentStatus status)
    {
        Assert.True(PaymentStateMachine.CanTransition(status, status));
    }

    #endregion

    #region CanTransition – valid transitions

    [Theory]
    [InlineData(PaymentStatus.Created, PaymentStatus.PendingProviderConfirmation)]
    [InlineData(PaymentStatus.Created, PaymentStatus.Succeeded)]
    [InlineData(PaymentStatus.Created, PaymentStatus.Failed)]
    [InlineData(PaymentStatus.PendingProviderConfirmation, PaymentStatus.Succeeded)]
    [InlineData(PaymentStatus.PendingProviderConfirmation, PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Succeeded, PaymentStatus.RefundPending)]
    [InlineData(PaymentStatus.RefundPending, PaymentStatus.Refunded)]
    [InlineData(PaymentStatus.RefundPending, PaymentStatus.RefundFailed)]
    [InlineData(PaymentStatus.RefundFailed, PaymentStatus.RefundPending)]
    public void CanTransition_ValidTransitions_ShouldReturnTrue(PaymentStatus from, PaymentStatus to)
    {
        Assert.True(PaymentStateMachine.CanTransition(from, to));
    }

    #endregion

    #region CanTransition – invalid transitions

    [Theory]
    [InlineData(PaymentStatus.Created, PaymentStatus.Refunded)]
    [InlineData(PaymentStatus.Created, PaymentStatus.RefundPending)]
    [InlineData(PaymentStatus.Created, PaymentStatus.RefundFailed)]
    [InlineData(PaymentStatus.Failed, PaymentStatus.Succeeded)]
    [InlineData(PaymentStatus.Failed, PaymentStatus.PendingProviderConfirmation)]
    [InlineData(PaymentStatus.Succeeded, PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Succeeded, PaymentStatus.Created)]
    [InlineData(PaymentStatus.Refunded, PaymentStatus.RefundPending)]
    [InlineData(PaymentStatus.Refunded, PaymentStatus.Created)]
    [InlineData(PaymentStatus.RefundPending, PaymentStatus.Created)]
    public void CanTransition_InvalidTransitions_ShouldReturnFalse(PaymentStatus from, PaymentStatus to)
    {
        Assert.False(PaymentStateMachine.CanTransition(from, to));
    }

    #endregion

    #region EnsureCanTransition

    [Fact]
    public void EnsureCanTransition_WithValidTransition_ShouldNotThrow()
    {
        var ex = Record.Exception(() =>
            PaymentStateMachine.EnsureCanTransition(PaymentStatus.Created, PaymentStatus.Succeeded));

        Assert.Null(ex);
    }

    [Fact]
    public void EnsureCanTransition_WithInvalidTransition_ShouldThrowInvalidPaymentStateTransitionException()
    {
        var ex = Assert.Throws<InvalidPaymentStateTransitionException>(() =>
            PaymentStateMachine.EnsureCanTransition(PaymentStatus.Failed, PaymentStatus.Succeeded));

        Assert.Contains("Failed", ex.Message);
        Assert.Contains("Succeeded", ex.Message);
    }

    [Fact]
    public void EnsureCanTransition_SameStatus_ShouldNotThrow()
    {
        var ex = Record.Exception(() =>
            PaymentStateMachine.EnsureCanTransition(PaymentStatus.Succeeded, PaymentStatus.Succeeded));

        Assert.Null(ex);
    }

    #endregion
}

public class RefundStateMachineTests
{
    #region CanTransition – same status (self-loop)

    [Theory]
    [InlineData(RefundStatus.Requested)]
    [InlineData(RefundStatus.PendingProviderConfirmation)]
    [InlineData(RefundStatus.Succeeded)]
    [InlineData(RefundStatus.Failed)]
    public void CanTransition_SameStatus_ShouldReturnTrue(RefundStatus status)
    {
        Assert.True(RefundStateMachine.CanTransition(status, status));
    }

    #endregion

    #region CanTransition – valid transitions

    [Theory]
    [InlineData(RefundStatus.Requested, RefundStatus.PendingProviderConfirmation)]
    [InlineData(RefundStatus.Requested, RefundStatus.Succeeded)]
    [InlineData(RefundStatus.Requested, RefundStatus.Failed)]
    [InlineData(RefundStatus.PendingProviderConfirmation, RefundStatus.Succeeded)]
    [InlineData(RefundStatus.PendingProviderConfirmation, RefundStatus.Failed)]
    [InlineData(RefundStatus.Failed, RefundStatus.PendingProviderConfirmation)]
    public void CanTransition_ValidTransitions_ShouldReturnTrue(RefundStatus from, RefundStatus to)
    {
        Assert.True(RefundStateMachine.CanTransition(from, to));
    }

    #endregion

    #region CanTransition – invalid transitions

    [Theory]
    [InlineData(RefundStatus.Succeeded, RefundStatus.Requested)]
    [InlineData(RefundStatus.Succeeded, RefundStatus.Failed)]
    [InlineData(RefundStatus.Succeeded, RefundStatus.PendingProviderConfirmation)]
    [InlineData(RefundStatus.Failed, RefundStatus.Succeeded)]
    [InlineData(RefundStatus.Failed, RefundStatus.Requested)]
    [InlineData(RefundStatus.PendingProviderConfirmation, RefundStatus.Requested)]
    public void CanTransition_InvalidTransitions_ShouldReturnFalse(RefundStatus from, RefundStatus to)
    {
        Assert.False(RefundStateMachine.CanTransition(from, to));
    }

    #endregion

    #region EnsureCanTransition

    [Fact]
    public void EnsureCanTransition_WithValidTransition_ShouldNotThrow()
    {
        var ex = Record.Exception(() =>
            RefundStateMachine.EnsureCanTransition(RefundStatus.Requested, RefundStatus.Succeeded));

        Assert.Null(ex);
    }

    [Fact]
    public void EnsureCanTransition_WithInvalidTransition_ShouldThrowInvalidRefundStateTransitionException()
    {
        var ex = Assert.Throws<InvalidRefundStateTransitionException>(() =>
            RefundStateMachine.EnsureCanTransition(RefundStatus.Succeeded, RefundStatus.Failed));

        Assert.Contains("Succeeded", ex.Message);
        Assert.Contains("Failed", ex.Message);
    }

    [Fact]
    public void EnsureCanTransition_SameStatus_ShouldNotThrow()
    {
        var ex = Record.Exception(() =>
            RefundStateMachine.EnsureCanTransition(RefundStatus.Requested, RefundStatus.Requested));

        Assert.Null(ex);
    }

    #endregion
}

using Application.Gateways.Exceptions;
using Grpc.Core;
using Infrastructure.Gateways;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.Payment;

namespace Infrastructure.Tests.Gateways;

public class PaymentGatewayTests
{
    private readonly PaymentService.PaymentServiceClient _client =
        Substitute.For<PaymentService.PaymentServiceClient>();

    private readonly ILogger<PaymentGateway> _logger =
        Substitute.For<ILogger<PaymentGateway>>();

    private PaymentGateway Build() => new(_client, _logger);

    // helpers
    
    private static AsyncUnaryCall<T> GrpcCall<T>(T response) =>
        new AsyncUnaryCall<T>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

    private static AsyncUnaryCall<T> GrpcFail<T>(StatusCode code, string detail) =>
        new AsyncUnaryCall<T>(
            Task.FromException<T>(new RpcException(new Status(code, detail))),
            Task.FromResult(new Metadata()),
            () => new Status(code, detail),
            () => new Metadata(),
            () => { });

    [Fact]
    public async Task ProcessPaymentAsync_ShouldReturnSucceededResult_WhenSucceeds()
    {
        var paymentId = "pay-123";
        _client
            .ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(new ProcessPaymentResponse
            {
                Success = true,
                PaymentId = paymentId,
                Status = (ProcessPaymentStatus)1
            }));

        var result = await Build().ProcessPaymentAsync(
            Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", "CARD", CancellationToken.None);

        Assert.Equal(paymentId, result.PaymentId);
        Assert.Equal(Application.Gateways.PaymentProcessingStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldReturnPendingResult_WhenProviderReturnsPending()
    {
        var paymentId = "pay-pending";
        _client
            .ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(new ProcessPaymentResponse
            {
                Success = true,
                PaymentId = paymentId,
                Status = (ProcessPaymentStatus)2,
                ProviderPaymentIntentId = "pi_123"
            }));

        var result = await Build().ProcessPaymentAsync(
            Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", "CARD", CancellationToken.None);

        Assert.Equal(paymentId, result.PaymentId);
        Assert.Equal(Application.Gateways.PaymentProcessingStatus.Pending, result.Status);
        Assert.Equal("pi_123", result.ProviderPaymentIntentId);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldReturnRequiresActionResult_WhenProviderRequiresAction()
    {
        var paymentId = "pay-3ds";
        _client
            .ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(new ProcessPaymentResponse
            {
                Success = true,
                PaymentId = paymentId,
                Status = (ProcessPaymentStatus)4,
                ProviderPaymentIntentId = "pi_3ds",
                ClientSecret = "cs_3ds"
            }));

        var result = await Build().ProcessPaymentAsync(
            Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", "CARD", CancellationToken.None);

        Assert.Equal(paymentId, result.PaymentId);
        Assert.Equal(Application.Gateways.PaymentProcessingStatus.RequiresAction, result.Status);
        Assert.Equal("pi_3ds", result.ProviderPaymentIntentId);
        Assert.Equal("cs_3ds", result.ClientSecret);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldThrowInsufficientFunds_WhenErrorCodeIsInsufficientFunds()
    {
        _client
            .ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(new ProcessPaymentResponse { Success = false, ErrorCode = "INSUFFICIENT_FUNDS" }));

        await Assert.ThrowsAsync<InsufficientFundsException>(() =>
            Build().ProcessPaymentAsync(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", "CARD", CancellationToken.None));
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldThrowPaymentDeclined_WhenErrorCodeIsPaymentDeclined()
    {
        _client
            .ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(new ProcessPaymentResponse { Success = false, ErrorCode = "PAYMENT_DECLINED" }));

        await Assert.ThrowsAsync<PaymentDeclinedException>(() =>
            Build().ProcessPaymentAsync(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", "CARD", CancellationToken.None));
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldThrowInvalidOperation_WhenUnknownErrorCode()
    {
        _client
            .ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(new ProcessPaymentResponse { Success = false, ErrorCode = "UNKNOWN_ERROR" }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Build().ProcessPaymentAsync(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", "CARD", CancellationToken.None));
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldThrowPaymentDeclined_WhenRpcInvalidArgument()
    {
        _client
            .ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcFail<ProcessPaymentResponse>(StatusCode.InvalidArgument, "bad payload"));

        await Assert.ThrowsAsync<PaymentDeclinedException>(() =>
            Build().ProcessPaymentAsync(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", "CARD", CancellationToken.None));
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldThrowInsufficientFunds_WhenRpcFailedPrecondition()
    {
        _client
            .ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcFail<ProcessPaymentResponse>(StatusCode.FailedPrecondition, "balance low"));

        await Assert.ThrowsAsync<InsufficientFundsException>(() =>
            Build().ProcessPaymentAsync(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", "CARD", CancellationToken.None));
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldThrowGatewayUnavailable_WithTimeoutReason_WhenRpcDeadlineExceeded()
    {
        _client
            .ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcFail<ProcessPaymentResponse>(StatusCode.DeadlineExceeded, "timeout"));

        var ex = await Assert.ThrowsAsync<GatewayUnavailableException>(() =>
            Build().ProcessPaymentAsync(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", "CARD", CancellationToken.None));

        Assert.Equal(GatewayUnavailableReason.Timeout, ex.Reason);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldThrowGatewayUnavailable_WithServiceUnavailableReason_WhenRpcUnavailable()
    {
        _client
            .ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcFail<ProcessPaymentResponse>(StatusCode.Unavailable, "service down"));

        var ex = await Assert.ThrowsAsync<GatewayUnavailableException>(() =>
            Build().ProcessPaymentAsync(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", "CARD", CancellationToken.None));

        Assert.Equal(GatewayUnavailableReason.ServiceUnavailable, ex.Reason);
    }

    [Fact]
    public async Task RefundAsync_ShouldReturnRefundId_WhenSucceeds()
    {
        var refundId = "ref-456";
        _client
            .RefundPaymentAsync(Arg.Any<RefundPaymentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(new RefundPaymentResponse { Success = true, RefundId = refundId }));

        var result = await Build().RefundAsync("pay-123", 50m, "duplicate", CancellationToken.None);

        Assert.Equal(refundId, result);
    }

    [Fact]
    public async Task RefundAsync_ShouldThrowInvalidOperation_WhenNotSuccess()
    {
        _client
            .RefundPaymentAsync(Arg.Any<RefundPaymentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(new RefundPaymentResponse { Success = false, ErrorMessage = "already refunded" }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Build().RefundAsync("pay-123", 50m, "duplicate", CancellationToken.None));
    }

    [Fact]
    public async Task RefundAsync_ShouldThrowInvalidOperation_WhenRpcNotFound()
    {
        _client
            .RefundPaymentAsync(Arg.Any<RefundPaymentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcFail<RefundPaymentResponse>(StatusCode.NotFound, "payment not found"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Build().RefundAsync("pay-xyz", 50m, "test", CancellationToken.None));
    }
}

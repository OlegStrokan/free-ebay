using Grpc.Core;
using payment_service.Entities;
using payment_service.Enums;
using payment_service.Services;
using Payment; // for PaymentService.PaymentServiceBase

namespace payment_service.Services;

public class PaymentGrpcService : PaymentService.PaymentServiceBase
{
    private readonly MyPaymentService _paymentService;
    private readonly ILogger<PaymentGrpcService> _logger;

    public PaymentGrpcService(MyPaymentService paymentService, ILogger<PaymentGrpcService> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    public override async Task<ProcessPaymentResponse> ProcessPayment(ProcessPaymentRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Processing payment via gRPC for order: {OrderId}", request.OrderId);

        try
        {
            if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.OrderId))
            {
                return new ProcessPaymentResponse
                {
                    PaymentId = request.Id,
                    Status = PaymentStatus.Failed.ToString(),
                    ErrorMessage = "Id and OrderId are required ULIDs from the upstream service."
                };
            }

            var payment = new PaymentEntity
            {
                Id = request.Id,
                OrderId = request.OrderId,
                Amount = new MoneyEntity
                {
                    Amount = request.Amount.Amount,
                    Currency = request.Amount.Currency,
                    Fraction = request.Amount.Fraction
                },
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = Enums.PaymentStatus.Pending
            };

            var result = await _paymentService.ProcessPayment(payment);

            return new ProcessPaymentResponse
            {
                PaymentId = result.Id,
                Status = result.PaymentStatus.ToString(),
                TransactionId = result.Id, // You might want to add a separate transaction ID field
                ClientSecret = result.ClientSecret ?? string.Empty,
                ErrorMessage = result.PaymentStatus == Enums.PaymentStatus.Failed ? "Payment processing failed" : string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment via gRPC for order: {OrderId}", request.OrderId);
            return new ProcessPaymentResponse
            {
                PaymentId = request.Id,
                Status = PaymentStatus.Failed.ToString(),
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<GetPaymentStatusResponse> GetPaymentStatus(GetPaymentStatusRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting payment status via gRPC for payment: {PaymentId}", request.PaymentId);

        try
        {
            // This would typically query your database for the payment status
            // For now, we'll return a mock response
            return new GetPaymentStatusResponse
            {
                PaymentId = request.PaymentId,
                Status = PaymentStatus.Pending.ToString(),
                OrderId = "mock-order-id",
                Amount = new Payment.Money // <-- Use the generated type here
                {
                    Amount = 1000,
                    Currency = "USD",
                    Fraction = 100
                },
                PaymentMethod = "Card"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status via gRPC for payment: {PaymentId}", request.PaymentId);
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task StreamPaymentUpdates(StreamPaymentUpdatesRequest request, IServerStreamWriter<PaymentUpdate> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("Starting payment status stream for payment: {PaymentId}", request.PaymentId);

        try
        {
            // Simulate payment status updates
            var statuses = new[] { "Pending", "Processing", "Completed" };
            
            foreach (var status in statuses)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                var update = new PaymentUpdate
                {
                    PaymentId = request.PaymentId,
                    Status = status,
                    Message = $"Payment status updated to {status}",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                await responseStream.WriteAsync(update);
                await Task.Delay(2000, context.CancellationToken); // Wait 2 seconds between updates
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming payment updates for payment: {PaymentId}", request.PaymentId);
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
} 
using Grpc.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Protos.Accounting;
using Protos.Inventory;
using Protos.Payment;

namespace Order.E2ETests.Infrastructure.Mocks;

public abstract class FakeGrpcServerBase : IAsyncDisposable
{
    private WebApplication? _app;
    public string Address { get; private set; } = string.Empty;

    protected abstract void RegisterServices(IServiceCollection services);
    protected abstract void MapServices(WebApplication app);

    public async Task StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();

        builder.WebHost.ConfigureKestrel(o =>
        {
            o.ListenLocalhost(0, lo => lo.Protocols = HttpProtocols.Http2);
        });

        builder.Services.AddGrpc();
        RegisterServices(builder.Services);

        _app = builder.Build();
        MapServices(_app);
    }

    public async Task StopAsync()
    {
        if (_app is not null)
            await _app.StopAsync();
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}


//---------Payment Server---------//

public class FakePaymentGrpcServer : FakeGrpcServerBase
{
    public bool ProcessShouldSucceed { get; set; } = true;
    public string PaymentIdToReturn { get; set; } = "PAY-DEFAULT";
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public bool RefundShouldSucceed { get; set; } = true;
    public string RefundIdToReturn { get; set; } = "REF-DEFAULT";

    public List<ProcessPaymentRequest> ProcessCalls { get; } = new();
    public List<RefundPaymentRequest> RefundCalls { get; } = new();

    public void Reset()
    {
        ProcessShouldSucceed = true;
        PaymentIdToReturn = "PAY-" + Guid.NewGuid().ToString()[..8];
        ErrorCode = string.Empty;
        ErrorMessage = string.Empty;
        RefundShouldSucceed = true;
        RefundIdToReturn = "REF-" + Guid.NewGuid().ToString()[..8];
        ProcessCalls.Clear();
        RefundCalls.Clear();
    }

    protected override void RegisterServices(IServiceCollection s) => s.AddSingleton(this);
    protected override void MapServices(WebApplication app)
    {
        app.MapGrpcService<FakePaymentGrpcServer>();
    }
}

public class FakePaymentServiceImpl : PaymentService.PaymentServiceBase
{
    private readonly FakePaymentGrpcServer _cfg;
    public FakePaymentServiceImpl(FakePaymentGrpcServer cfg) => _cfg = cfg;

    public override Task<ProcessPaymentResponse> ProcessPayment(
        ProcessPaymentRequest request, 
        ServerCallContext context
        )
    {
        _cfg.ProcessCalls.Add(request);

        if (!_cfg.ProcessShouldSucceed)
            return Task.FromResult(new ProcessPaymentResponse
            {
                Success = false,
                ErrorCode = string.IsNullOrEmpty(_cfg.ErrorCode) ? "PAYMENT_DECLINED" : _cfg.ErrorCode,
                ErrorMessage = string.IsNullOrEmpty(_cfg.ErrorMessage) ? "Card declined" : _cfg.ErrorMessage
            });

        return Task.FromResult(new ProcessPaymentResponse
        {
            Success = true,
            PaymentId = _cfg.PaymentIdToReturn
        });
    }

    public override Task<ProcessPaymentResponse> RefundPayment(
        RefundPaymentRequest request,
        ServerCallContext context
        )
    {
        _cfg.RefundCalls.Add(request);

        if (!_cfg.RefundShouldSucceed)
            return Task.FromResult(new ProcessPaymentResponse
            {
                Success = false,
                ErrorMessage = "Refund failed"
            });

        return Task.FromResult(new ProcessPaymentResponse
        {
            Success = true,
            PaymentId = _cfg.RefundIdToReturn
        });
    }
}

//---------Inventory Server---------//

public class FakeInventoryGrpcServer : FakeGrpcServerBase
{
    public bool ReserveShouldSucceed { get; set; } = true;
    public string ReservationIdToReturn { get; set; } = "RES-DEFAULT";
    public string ReserveErrorMessage { get; set; } = string.Empty;
    public bool ReleaseShouldSucceed { get; set; } = true;

    public List<ReserveInventoryRequest> ReserveCalls { get; } = new();
    public List<ReleaseInventoryRequest> ReleaseCalls { get; } = new();

    public void Reset()
    {
        ReserveShouldSucceed = true;
        ReservationIdToReturn = "RES-" + Guid.NewGuid().ToString()[..8];
        ReserveErrorMessage = string.Empty;
        ReleaseShouldSucceed = true;
        ReserveCalls.Clear();
        ReleaseCalls.Clear();
    }

    protected override void RegisterServices(IServiceCollection s) => s.AddSingleton(this);
    protected override void MapServices(WebApplication app)
    {
        app.MapGrpcService<FakeInventoryGrpcServer>();
    }
}


public class FakeInventoryServiceImpl : InventoryService.InventoryServiceBase
{
    private readonly FakeInventoryGrpcServer _cfg;
    public FakeInventoryServiceImpl(FakeInventoryGrpcServer cfg) => _cfg = cfg;

    public override Task<ReserveInventoryResponse> ReserveInventory(
        ReserveInventoryRequest request,
        ServerCallContext context
        )
    {
        _cfg.ReserveCalls.Add(request);

        if (!_cfg.ReserveShouldSucceed)
            return Task.FromResult(new ReserveInventoryResponse
            {
                Success = false,
                ErrorMessage = string.IsNullOrEmpty(_cfg.ReserveErrorMessage)
                    ? "Insufficient inventory"
                    : _cfg.ReserveErrorMessage
            });

        return Task.FromResult(new ReserveInventoryResponse
        {
            Success = true,
            ReservationId = _cfg.ReservationIdToReturn
        });
    }

    public override Task<ReleaseInventoryResponse> ReleaseInventory(
        ReleaseInventoryRequest request,
        ServerCallContext context)

    {
        _cfg.ReleaseCalls.Add(request);

        return Task.FromResult(new ReleaseInventoryResponse
        {
            Success = _cfg.ReleaseShouldSucceed,
            ErrorMessage = _cfg.ReleaseShouldSucceed ? string.Empty : "Release failed"
        });
    }
}

//---------Accounting Server---------//

public class FakeAccountingGrpcServer : FakeGrpcServerBase
{
    public bool ShouldSucceed { get; set; } = true;
    public string TransactionIdToReturn { get; set; } = "TXN-DEFAULT";
    public string ReversalIdToReturn { get; set; } = "REV_DEFAULT";

    public List<RecordRefundRequest> RecordRefundCalls { get; } = new();
    public List<ReverseRevenueRequest> ReverseRevenueCalls { get; } = new();

    public void Reset()
    {
        ShouldSucceed = true;
        TransactionIdToReturn = "TXN-" + Guid.NewGuid().ToString()[..8];
        ReversalIdToReturn = "REV-" + Guid.NewGuid().ToString()[..8];
        RecordRefundCalls.Clear();
        ReverseRevenueCalls.Clear();
    }

    protected override void RegisterServices(IServiceCollection s) => s.AddSingleton(this);
    protected override void MapServices(WebApplication app)
    {
        app.MapGrpcService<FakeAccountingGrpcServer>();
    }
}

public class FakeAccountingServiceImpl : AccountingService.AccountingServiceBase
{
    private readonly FakeAccountingGrpcServer _cfg;
    
    public FakeAccountingServiceImpl(FakeAccountingGrpcServer cfg) => _cfg = cfg;

    public override Task<RecordRefundResponse> RecordRefund(
        RecordRefundRequest request,
        ServerCallContext context
    )
    {
        _cfg.RecordRefundCalls.Add(request);

        return Task.FromResult(new RecordRefundResponse
        {
            Success = _cfg.ShouldSucceed,
            TransactionId = _cfg.ShouldSucceed ? _cfg.TransactionIdToReturn : string.Empty,
            ErrorMessage = _cfg.ShouldSucceed ? string.Empty : "RecordRefund failed"
        });
    }
}


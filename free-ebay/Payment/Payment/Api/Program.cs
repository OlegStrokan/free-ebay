using Api.Endpoints;
using Api.GrpcServices;
using Api.Middleware;
using Application;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ExceptionHandlingInterceptor>();
});

builder.Services.AddSingleton<ExceptionHandlingInterceptor>();
builder.Services.AddGrpcHealthChecks();

var app = builder.Build();

app.MapGrpcService<PaymentGrpcService>();
app.MapGrpcHealthChecksService();

app.MapStripeWebhookEndpoint();
app.MapAdminOrderCallbackEndpoint();

app.Run();

public partial class Program;
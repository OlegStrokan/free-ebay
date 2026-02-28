using Api.GrpcServices;
using Application;
using FluentValidation;
using Infrastructure;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Protos.Order;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();
builder.Services.AddScoped<IValidator<RequestReturnRequest>, RequestReturnRequestValidator>();

builder.Services.AddGrpc();

builder.Services.AddOpenTelemetry()
    .WithTracing(b => b
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("order-service"))
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("OrderService.Kafka")
        .AddOtlpExporter(o =>
            o.Endpoint = new Uri(
                builder.Configuration["Otel:Endpoint"] ?? "http://localhost:4317")));

var app = builder.Build();

app.MapGrpcService<OrderGrpcService>();

app.Run();

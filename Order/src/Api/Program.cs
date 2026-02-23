using Api.GrpcServices;
using Application;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<OrderGrpcService>();

app.Run();


using payment_service.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add gRPC services
builder.Services.AddGrpc();

// Add existing services
builder.Services.AddScoped<MyPaymentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Map both HTTP controllers and gRPC services
app.MapControllers();
app.MapGrpcService<PaymentGrpcService>();

app.Run();
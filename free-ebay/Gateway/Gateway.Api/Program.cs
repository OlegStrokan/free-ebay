using Gateway.Api.Endpoints;
using Gateway.Api.Extensions;
using Gateway.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = !builder.Environment.IsDevelopment()
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddGrpcClients(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Free eBay Gateway API",
        Version = "v1"
    });
});

builder.Services.AddExceptionHandler<GrpcExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapRoleEndpoints();
app.MapProductEndpoints();
app.MapOrderEndpoints();
app.MapB2BOrderEndpoints();
app.MapRecurringOrderEndpoints();
app.MapPaymentEndpoints();
app.MapInventoryEndpoints();
app.MapSearchEndpoints();

app.Run();

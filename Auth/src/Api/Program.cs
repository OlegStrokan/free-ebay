using Api.GrpcServices;
using Application.UseCases.Login;
using Application.UseCases.RefreshToken;
using Application.UseCases.Register;
using Application.UseCases.RequestPasswordReset;
using Application.UseCases.ResetPassword;
using Application.UseCases.RevokeToken;
using Application.UseCases.ValidateToken;
using Application.UseCases.VerifyEmail;
using Domain.Gateways;
using Domain.Repositories;
using Infrastructure.Gateways;
using Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// repositories
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

// use cases
builder.Services.AddScoped<IRegisterUseCase, RegisterUseCase>();
builder.Services.AddScoped<ILoginUseCase, LoginUseCase>();
builder.Services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
builder.Services.AddScoped<IRequestPasswordResetUseCase, RequestPasswordResetUseCase>();
builder.Services.AddScoped<IRevokeTokenUseCase, RevokeTokenUseCase>();
builder.Services.AddScoped<IValidateTokenUseCase,  ValidateTokenUseCase>();
builder.Services.AddScoped<IVerifyEmailUseCase, VerifyEmailUseCase>();
builder.Services.AddScoped<IResetPasswordUseCase, ResetPasswordUseCase>();

// external systems
builder.Services.AddScoped<IUserGateway, UserGateway>();

// api
builder.Services.AddScoped<RegisterGrpcService>();
builder.Services.AddScoped<LoginGrpcService>();
builder.Services.AddScoped<RefreshTokenGrpcService>();
builder.Services.AddScoped<RequestPasswordResetGrpcService>();
builder.Services.AddScoped<ResetPasswordGrpcService>();
builder.Services.AddScoped<ValidateTokenGrpcService>();
builder.Services.AddScoped<VerifyEmailGrpcService>();
builder.Services.AddScoped<RevokeTokenGrpcService>();
    

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
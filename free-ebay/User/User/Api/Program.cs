using Api.GrpcServices;
using Domain.Common.Interfaces;
using Application.UseCases.BlockUser;
using Application.UseCases.CreateUser;
using Application.UseCases.DeleteUser;
using Application.UseCases.GetUserById;
using Application.UseCases.UpdatePassword;
using Application.UseCases.UpdateUser;
using Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Domain.Repositories;
using Infrastructure.Helpers;
using Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var postgresConnectionString =
	builder.Configuration.GetConnectionString("Postgres")
	?? throw new InvalidOperationException("ConnectionStrings:Postgres is required");

builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(postgresConnectionString));

builder.Services.AddScoped<ICreateUserUseCase, CreateUserUseCase>();
builder.Services.AddScoped<IUpdateUserUseCase, UpdateUserUseCase>();
builder.Services.AddScoped<IGetUserByIdUseCase, GetUserByIdUseCase>();
builder.Services.AddScoped<IDeleteUserUseCase, DeleteUserUseCase>();
builder.Services.AddScoped<IBlockUserUseCase, BlockUserUseCase>();
builder.Services.AddScoped<IUpdatePasswordUseCase, UpdatePasswordUseCase>();

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

builder.Services.AddGrpc();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	await db.Database.EnsureCreatedAsync();
}

app.MapGrpcService<UserGrpcService>();



app.MapGet("/", () => "Hello World!");

app.Run();

public partial class Program;
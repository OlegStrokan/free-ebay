using Api.GrpcServices;
using Domain.Common.Interfaces;
using Application.UseCases.CreateUser;
using Application.UseCases.DeleteUser;
using Application.UseCases.GetUserById;
using Application.UseCases.UpdateUser;
using Domain.Repositories;
using Infrastructure.Helpers;
using Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ICreateUserUseCase, CreateUserUseCase>();
builder.Services.AddScoped<IUpdateUserUseCase, UpdateUserUseCase>();
builder.Services.AddScoped<IGetUserByIdUseCase, GetUserByIdUseCase>();
builder.Services.AddScoped<IDeleteUserUseCase, DeleteUserUseCase>();

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IIdGenerator, IdGenerator>();

builder.Services.AddGrpc();


var app = builder.Build();

app.MapGrpcService<CreateUserGrpcService>();  
app.MapGrpcService<UpdateUserGrpcService>();  
app.MapGrpcService<GetUserByIdGrpcService>();
app.MapGrpcService<DeleteUserGrpcService>();



app.MapGet("/", () => "Hello World!");

app.Run();
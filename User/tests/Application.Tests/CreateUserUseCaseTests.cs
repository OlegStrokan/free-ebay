using System;
using System.Threading.Tasks;
using Application.UseCases.CreateUser;
using Domain.Common.Interfaces;
using Domain.Entities.User;
using Domain.Repositories;

using NSubstitute;
using Xunit;

namespace Application.Tests;

public class CreateUserUseCaseTests
{
    [Fact]
    public async Task ShouldCreateUserAndReturnResponse()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var generatedId = "ULID_!@*()!(*@";
        
        idGenerator.GenerateId() .Returns(generatedId);
        
        var command = new CreateUserCommand("testuser@email.com", "password", "Oleh Strokan", "+420");

        var savedUser = new UserEntity
        {
            Id = generatedId,
            Email = command.Email,
            CreatedAt = DateTime.UtcNow,
            Fullname = command.Fullname,
            Password = command.Password,
            Phone = command.Phone,
            // status will be default as ACTIVE
        };
    
        
        userRepository.CreateUser(Arg.Any<UserEntity>()).Returns(savedUser);

        var useCase = new CreateUserUseCase(userRepository, idGenerator);

        var result = await useCase.ExecuteAsync(command);
        
        Assert.Equal(savedUser.Id, result.Id);
        Assert.Equal(savedUser.Email, result.Email);
        Assert.Equal(savedUser.Fullname, result.Fullname);
        Assert.Equal(savedUser.Status, result.Status);

        await userRepository.Received(1).CreateUser(Arg.Is<UserEntity>(u =>

            u.Id == generatedId &&
            u.Email == command.Email &&
            u.Password == command.Password &&
            u.Fullname == command.Fullname &&
            u.Phone == command.Phone
        ));
    }
}
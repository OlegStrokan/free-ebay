using Application.UseCases.Register;
using Domain.Common.Interfaces;
using Domain.Entities;
using Domain.Gateways;
using Domain.Repositories;
using NSubstitute;

namespace Application.Tests;

public class RegisterUseCaseTests
{
    [Fact]
    public async Task ShouldRegisterUserAndCreateVerificationToken()
    {
        var userGateway = Substitute.For<IUserGateway>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var emailVerificationTokenRepository = Substitute.For<IEmailVerificationTokenRepository>();
        var generatedId = "ULID_GENERATED_USER";
        var generatedTokenId = "ULID_GENERATED_TOKEN";

        idGenerator.GenerateId().Returns(generatedId);
         userGateway.CreateUserAsync(
            Arg.Any<string>(), 
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>()
            ).Returns(generatedId);

            var command = new RegisterCommand(
                "test@example.com",
                "password123",
                "John Doe",
                "+1234567890");

            var useCase = new RegisterUseCase(emailVerificationTokenRepository, idGenerator, userGateway);

            var result = await useCase.ExecuteAsync(command);
            
            Assert.Equal(generatedId, result.UserId);
            Assert.Equal(command.Email, result.Email);
            Assert.Equal(command.Fullname, result.Fullname);
            Assert.NotNull(result.VerificationToken);
            //@todo shitty test, use constant or whatever
            Assert.Equal("User registered successfully. Please verify email", result.Message);
            
            
            
            await userGateway.Received(1).CreateUserAsync(command.Email, command.Fullname, command.Password, command.Phone);

            await emailVerificationTokenRepository.Received(1).Received(1).CreateAsync(
                Arg.Is<EmailVerificationTokenEntity>(t =>
                    t.Id == generatedTokenId &&
                        t.UserId == generatedId &&
                        !string.IsNullOrEmpty(t.Token) &&
                        t.ExpiresAt > DateTime.UtcNow &&
                        t.IsUsed == false
                ));
    }
}
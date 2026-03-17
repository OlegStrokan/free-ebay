using Domain.Common.Interfaces;
using Domain.Entities;
using Domain.Gateways;
using Domain.Repositories;

namespace Application.UseCases.Register;

public class RegisterUseCase(
    IEmailVerificationTokenRepository verificationTokenRepository, 
    IIdGenerator idGenerator,
    IUserGateway userGateway
    ) : IRegisterUseCase
{

    public async Task<RegisterResponse> ExecuteAsync(RegisterCommand command)
    {
        var hashedPassword = command.Password;

 
        var userId = await userGateway.CreateUserAsync(
            email: command.Email,
            hashedPassword: hashedPassword,
            fullName: command.Fullname,
            phone: command.Phone
        );


        var verificationToken = new EmailVerificationTokenEntity
        {
            Id = idGenerator.GenerateId(),
            UserId = userId,
            //@think: JWT, not GUID 😃🤡
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };

        await verificationTokenRepository.CreateAsync(verificationToken);
        
        // @todo: send verification email via email service 

        return new RegisterResponse(userId, command.Email, command.Fullname, verificationToken.Token, "User registered successfully. Please verify your email");
    }
}
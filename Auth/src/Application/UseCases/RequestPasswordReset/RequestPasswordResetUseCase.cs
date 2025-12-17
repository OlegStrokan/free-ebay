using Domain.Common.Interfaces;
using Domain.Entities;
using Domain.Gateways;
using Domain.Repositories;

namespace Application.UseCases.RequestPasswordReset;

public class RequestPasswordResetUseCase(
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IIdGenerator idGenerator,
    IUserGateway userGateway)
{
    public async Task<RequestPasswordResetResponse> ExecuteAsync(RequestPasswordResetCommand command)
    {

        var user = await userGateway.GetUserByEmailAsync(command.Email);

        if (user == null)
        {
            return new RequestPasswordResetResponse(
                true,
                "If the email exists, a password reset link has been sent",
                null);
        }

        await passwordResetTokenRepository.DeleteByUserIdAsync(user.Id);

        var resetToken = new PasswordResetTokenEntity
        {
            Id = idGenerator.GenerateId(),
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
            IpAddress = command.IpAddress
        };

        await passwordResetTokenRepository.CreateAsync(resetToken);
        
        
        // @todo: send password reset email via email service

        return new RequestPasswordResetResponse(
            true,
            "Password reset link has been sent to your email",
            resetToken.Token
        );

    }
}
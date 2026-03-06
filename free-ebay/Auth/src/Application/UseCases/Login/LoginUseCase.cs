using Application.Common.Interfaces;
using Domain.Common.Interfaces;
using Domain.Entities;
using Domain.Gateways;
using Domain.Repositories;

namespace Application.UseCases.Login;

public class LoginUseCase
    (
        IRefreshTokenRepository refreshTokenRepository,
        IIdGenerator idGenerator,
        IUserGateway userGateway,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jtwTokenGenerator
) : ILoginUseCase
{
    
    private readonly DateTime _refreshTokenExpiresInDays = DateTime.Today.AddDays(30);

    public async Task<LoginResponse> ExecuteAsync(LoginCommand command)
    {
        var user = await userGateway.GetUserByEmailAsync(command.Email);

        if (user == null || !passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // @todo: uncomment when user service will support it
        // if (!user.isEmailVerified)
        // 
        //     throw new UnauthorizedAccessException("Invalid email or password");
        // }

        if (user.Status == UserStatus.Blocked)
        {
            throw new UnauthorizedAccessException("Your account has been blocked");
        }


        var accessToken = jtwTokenGenerator.GenerateAccessToken(user.Id, user.Email);

        var refreshTokenValue = jtwTokenGenerator.GenerateRefreshToken();

        var refreshToken = new RefreshTokenEntity
        {
            Id = idGenerator.GenerateId(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = _refreshTokenExpiresInDays,
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await refreshTokenRepository.CreateAsync(refreshToken);

        return new LoginResponse(accessToken, refreshTokenValue, 3600, "Bearer");
    }
}
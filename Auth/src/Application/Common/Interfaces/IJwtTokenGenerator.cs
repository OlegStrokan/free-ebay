namespace Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    // @todo: add roles after user service will support it
    string GenerateAccessToken(string userId, string email);
    string GenerateRefreshToken();
}
using Application.UseCases.RefreshToken;
using Grpc.Core;
using Protos.Auth;
using RefreshTokenResponse = Protos.Auth.RefreshTokenResponse;

namespace Api.GrpcServices;

public class RefreshTokenGrpcService(
    ILogger<RefreshTokenGrpcService> logger,
    RefreshTokenUseCase refreshTokenUseCaseUseCase
) : AuthService.AuthServiceBase
{
    public override async Task<RefreshTokenResponse> RefreshToken(RefreshTokenRequest request, ServerCallContext context)
    {
        try
        {
            logger.LogInformation("RefreshToken request received");

            var command = new RefreshTokenCommand(request.RefreshToken);

            var response = await refreshTokenUseCaseUseCase.ExecuteAsync(command);

            return new RefreshTokenResponse
            {
                AccessToken = response.AccessToken,
                ExpiresIn = response.ExpiresIn
            };
        }

        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning("Token refresh failed: {Message}", ex.Message);
            throw new RpcException(new Status(StatusCode.Unauthenticated, ex.Message));
        }

        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing token");
            throw new RpcException(new Status(StatusCode.Internal, "Token  refresh failed"));
        }
    }

}
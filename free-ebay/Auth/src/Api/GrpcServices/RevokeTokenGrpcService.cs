using Application.UseCases.RevokeToken;
using Grpc.Core;
using Protos.Auth;
using RevokeTokenResponse = Protos.Auth.RevokeTokenResponse;

namespace Api.GrpcServices;

public class RevokeTokenGrpcService (
    ILogger<RevokeTokenGrpcService> logger,
    IRevokeTokenUseCase revokeTokenUseCase) : AuthService.AuthServiceBase
{
    public override async Task<RevokeTokenResponse> RevokeToken(RevokeTokenRequest request, ServerCallContext context)
    {
        try
        {
            logger.LogInformation("RevokeToken request received");

            var command = new RevokeTokenCommand(request.RefreshToken);
            var response = await revokeTokenUseCase.ExecuteAsync(command);

            return new RevokeTokenResponse
            {
                Success = response.Success,
                Message = response.Message
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error revoking token");
            throw new RpcException(new Status(StatusCode.Internal, "Token revocation failed"));
        }
    }

}
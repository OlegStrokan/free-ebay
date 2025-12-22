using Application.UseCases.ValidateToken;
using Grpc.Core;
using Protos.Auth;
using ValidateTokenResponse = Protos.Auth.ValidateTokenResponse;

namespace Api.GrpcServices;

public class ValidateTokenGrpcService(
    ILogger<LoginService> logger,
    ValidateTokenUseCase validateTokenUseCase
)
    : AuthService.AuthServiceBase
{
    public override async Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
    {
        try
        {

            var command = new ValidateTokenCommand(request.AccessToken);
            var response = await validateTokenUseCase.ExecuteAsync(command);

            return new ValidateTokenResponse
            {
                IsValid = response.IsValid,
                UserId = response.UserId
            };

            // @todo add when user service will support roles
            // grpcResponse.Roles.AddRange(responseRoles);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validation token");
            //@think: instead of hardcode detail message check if we can use ex.Message
            throw new RpcException(new Status(StatusCode.Internal, "Token validation failed"));
        }
    }
}
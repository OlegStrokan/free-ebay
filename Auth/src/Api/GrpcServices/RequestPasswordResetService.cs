using Application.UseCases.RequestPasswordReset;
using Grpc.Core;
using Protos.Auth;
using RequestPasswordResetResponse = Protos.Auth.RequestPasswordResetResponse;

namespace Api.GrpcServices;

public class RequestPasswordResetService(
    ILogger<RegisterService> logger,
    RequestPasswordResetUseCase requestPasswordResetUseCase
) : AuthService.AuthServiceBase
{
    public override async Task<RequestPasswordResetResponse> RequestPasswordReset(RequestPasswordResetRequest request, ServerCallContext context)
    {
        try
        {
            logger.LogInformation("RequestPasswordReset request received for email: {Email}", request.Email);

            var command = new RequestPasswordResetCommand(request.Email);
            var response = await requestPasswordResetUseCase.ExecuteAsync(command);

            return new RequestPasswordResetResponse
            {
                Success = response.Success,
                Message = response.Message
            };

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting password");
            throw new RpcException(new Status(StatusCode.Internal, "Password reset request failed"));
        }
    }
}
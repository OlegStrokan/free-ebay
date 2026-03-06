using Application.UseCases.ResetPassword;
using Grpc.Core;
using Protos.Auth;
using ResetPasswordResponse = Protos.Auth.ResetPasswordResponse;

namespace Api.GrpcServices;

public class ResetPasswordGrpcService (
    ILogger<VerifyEmailGrpcService> logger,
    IResetPasswordUseCase resetPasswordUseCase) : AuthService.AuthServiceBase
{
    public override async Task<ResetPasswordResponse> ResetPassword(ResetPasswordRequest request, ServerCallContext context)
    {

        try
        {
            logger.LogInformation("Reset password request received");

            var command = new ResetPasswordCommand(request.Token, request.NewPassword);
            var response = await resetPasswordUseCase.ExecuteAsync(command);

            return new ResetPasswordResponse
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
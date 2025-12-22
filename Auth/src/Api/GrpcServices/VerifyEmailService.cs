using Application.UseCases.VerifyEmail;
using Grpc.Core;
using Protos.Auth;
using VerifyEmailResponse = Protos.Auth.VerifyEmailResponse;

namespace Api.GrpcServices;

public class VerifyEmailService (
    ILogger<VerifyEmailService> logger,
    VerifyEmailUseCase verifyEmailUseCase) : AuthService.AuthServiceBase
{
    public override async Task<VerifyEmailResponse> VerifyEmail(VerifyEmailRequest request, ServerCallContext context)
    {
        try
        {

            logger.LogInformation("VerifyEmail request received for token");

            var command = new VerifyEmailCommand(request.Token);
            var response = await verifyEmailUseCase.ExecuteAsync(command);

            return new VerifyEmailResponse
            {
                Success = response.Success,
                UserId = response.UserId,
                Message = response.Message
            };

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying email");
            throw new RpcException(new Status(StatusCode.Internal, "Email verification failed"));
        }
    }
}
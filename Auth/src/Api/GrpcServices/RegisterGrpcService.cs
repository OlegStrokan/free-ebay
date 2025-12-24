using Application.UseCases.Register;
using Grpc.Core;
using Protos.Auth;
using RegisterResponse = Protos.Auth.RegisterResponse;

namespace Api.GrpcServices;

public class RegisterGrpcService(
    ILogger<RegisterGrpcService> logger,
    IRegisterUseCase registerUseCase
) : AuthService.AuthServiceBase
{
    public override async Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        try
        {
            logger.LogInformation("Register request received for email: {Email}", request.Email);
            var command = new RegisterCommand(request.Email, request.Password, request.FullName, request.Phone);
            var response = await registerUseCase.ExecuteAsync(command);

            return new RegisterResponse {
                UserId = response.UserId,
                Email = response.Email,
                FullName = response.Fullname,
                Message = response.Message,
            };
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Registration failed: {Message}", ex.Message);
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during registration");
            throw new RpcException(new Status(StatusCode.Internal, "Registration failed"));
        }
    }
}
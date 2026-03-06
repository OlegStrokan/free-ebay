using Application.UseCases.Login;
using Grpc.Core;
using Protos.Auth;
using LoginResponse = Protos.Auth.LoginResponse;


namespace Api.GrpcServices;

public class LoginGrpcService(
    ILogger<LoginGrpcService> logger,
    ILoginUseCase loginUseCase
) : AuthService.AuthServiceBase
{
    public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        try
        {
            logger.LogInformation("Login request received for email: {Email}", request.Email);
            var command = new LoginCommand(request.Email, request.Password);
            var response = await loginUseCase.ExecuteAsync(command);

            return new LoginResponse {
                AccessToken = response.AccessToken,
                ExpiresIn = response.ExpiresIn,
                RefreshToken = response.RefreshToken,
                TokenType = response.TokenType
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Login failed for email: {Email}", request.Email);
            throw new RpcException(new Status(StatusCode.Unauthenticated, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login");
            throw new RpcException(new Status(StatusCode.Internal, "Login failed"));
        }
    }
}
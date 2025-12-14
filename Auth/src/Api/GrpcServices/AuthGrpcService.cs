using Application.UseCases.Login;
using Application.UseCases.RefreshToken;
using Application.UseCases.Register;
using Application.UseCases.RequestPasswordReset;
using Application.UseCases.ResetPassword;
using Application.UseCases.RevokeToken;
using Application.UseCases.ValidateToken;
using Application.UseCases.VerifyEmail;
using Grpc.Core;
using Protos.Auth;
using LoginResponse = Protos.Auth.LoginResponse;
using RefreshTokenResponse = Protos.Auth.RefreshTokenResponse;
using RegisterResponse = Protos.Auth.RegisterResponse;
using RequestPasswordResetResponse = Protos.Auth.RequestPasswordResetResponse;
using ResetPasswordResponse = Protos.Auth.ResetPasswordResponse;
using RevokeTokenResponse = Protos.Auth.RevokeTokenResponse;
using ValidateTokenResponse = Protos.Auth.ValidateTokenResponse;
using VerifyEmailResponse = Protos.Auth.VerifyEmailResponse;

namespace Api.GrpcServices;

// fuck mappers
public class AuthGrpcService(
    ILogger<AuthGrpcService> logger,
    RegisterUseCase registerUseCase,
    LoginUseCase loginUseCase,
    RevokeTokenUseCase revokeTokenUseCase,
    RefreshTokenUseCase refreshTokenUseCase,
    ValidateTokenUseCase validateTokenUseCase,
    VerifyEmailUseCase verifyEmailUseCase,
    RequestPasswordResetUseCase requestPasswordResetUseCase,
    ResetPasswordUseCase resetPasswordUseCase
) : AuthService.AuthServiceBase
{

    public override async Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {

        try
        {
            logger.LogInformation("Register request received for email: {Email}", request.Email);

            var command = new RegisterCommand(
                request.Email,
                request.Password,
                request.FullName,
                request.Phone);


            var response = await registerUseCase.ExecuteAsync(command);

            return new RegisterResponse
            {
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
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }


    }

    public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        try
        {
            logger.LogInformation("Login request received for email: {Email}", request.Email);

            var command = new LoginCommand(request.Email, request.Password);

            var response = await loginUseCase.ExecuteAsync(command);

            return new LoginResponse
            {
                AccessToken = response.AccessToken,
                ExpiresIn = response.ExpiresIn,
                RefreshToken = response.RefreshToken,
                TokenType = response.TokenType
            };


        }

        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Login failed for email: {Email} - {Message}", request.Email, ex.Message);
            throw new RpcException(new Status(StatusCode.Unauthenticated, ex.Message));
        }

        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login");
            throw new RpcException(new Status(StatusCode.Internal, "Login failed"));
        }
    }

    public override async Task<RefreshTokenResponse> RefreshToken(RefreshTokenRequest request, ServerCallContext context)
    {
        try
        {
            logger.LogInformation("RefreshToken request received");

            var command = new RefreshTokenCommand(request.RefreshToken);

            var response = await refreshTokenUseCase.ExecuteAsync(command);

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

    


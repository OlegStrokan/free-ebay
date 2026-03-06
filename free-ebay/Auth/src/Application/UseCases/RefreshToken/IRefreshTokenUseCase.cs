namespace Application.UseCases.RefreshToken;

public interface IRefreshTokenUseCase 
{
    Task<RefreshTokenResponse> ExecuteAsync(RefreshTokenCommand command);
}

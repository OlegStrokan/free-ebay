namespace Application.UseCases.RevokeToken;

public interface IRevokeTokenUseCase 
{
    Task<RevokeTokenResponse> ExecuteAsync(RevokeTokenCommand command);
}

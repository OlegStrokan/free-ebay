namespace Application.UseCases.ValidateToken;

public interface IValidateTokenUseCase 
{
    Task<ValidateTokenResponse> ExecuteAsync(ValidateTokenCommand command);
}

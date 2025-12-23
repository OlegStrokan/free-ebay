namespace Application.UseCases.RequestPasswordReset;

public interface IRequestPasswordResetUseCase 
{
    Task<RequestPasswordResetResponse> ExecuteAsync(RequestPasswordResetCommand command);
}

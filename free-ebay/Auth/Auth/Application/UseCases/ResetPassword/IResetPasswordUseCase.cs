namespace Application.UseCases.ResetPassword;

public interface IResetPasswordUseCase 
{
    Task<ResetPasswordResponse> ExecuteAsync(ResetPasswordCommand command);
}

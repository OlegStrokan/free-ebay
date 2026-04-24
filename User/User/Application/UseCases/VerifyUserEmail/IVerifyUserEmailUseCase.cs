namespace Application.UseCases.VerifyUserEmail;

public interface IVerifyUserEmailUseCase
{
    Task<bool> ExecuteAsync(string userId);
}

namespace Application.UseCases.VerifyCredentials;

public interface IVerifyCredentialsUseCase
{
    Task<VerifyCredentialsResponse?> ExecuteAsync(string email, string password);
}
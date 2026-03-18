namespace Application.UseCases.GetUserByEmail;

public interface IGetUserByEmailUseCase
{
    Task<GetUserByEmailResponse?> ExecuteAsync(string email);
}

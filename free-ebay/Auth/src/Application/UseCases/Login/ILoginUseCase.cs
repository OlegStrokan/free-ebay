
namespace Application.UseCases.Login;

public interface ILoginUseCase 
{
    Task<LoginResponse> ExecuteAsync(LoginCommand command);
}

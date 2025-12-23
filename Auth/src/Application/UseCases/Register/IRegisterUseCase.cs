namespace Application.UseCases.Register;

public interface IRegisterUseCase 
{
    Task<RegisterResponse> ExecuteAsync(RegisterCommand command);
}

namespace Application.UseCases.UpdatePassword;

public interface IUpdatePasswordUseCase
{
    Task ExecuteAsync(UpdatePasswordCommand command);
}

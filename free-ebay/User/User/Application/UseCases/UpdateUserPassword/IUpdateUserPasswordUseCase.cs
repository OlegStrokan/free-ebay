namespace Application.UseCases.UpdateUserPassword;

public interface IUpdateUserPasswordUseCase
{
    Task<UpdateUserPasswordResult> ExecuteAsync(UpdateUserPasswordCommand command);
}

namespace Application.UseCases.UpdateUser;

public interface IUpdateUserUseCase
{
    Task<UpdateUserResponse> ExecuteAsync(UpdateUserCommand command);
}
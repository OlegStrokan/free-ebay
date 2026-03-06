namespace Application.UseCases.CreateUser;

public interface ICreateUserUseCase
{
    Task<CreateUserResponse> ExecuteAsync(CreateUserCommand command);
}
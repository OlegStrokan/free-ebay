namespace Application.UseCases.DeleteUser;

public interface IDeleteUserUseCase
{
    Task ExecuteAsync(string id);
}